using backend.Models;
using backend.Repositories;
using IocNodes.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace IocNodes.Services
{
    public class DataFeedService : IDataFeedService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IIocNodeService _iocService;
        private readonly IIocNodeRepository _iocRepo;
        private readonly ILogger<DataFeedService> _logger;

        public DataFeedService(
            IHttpClientFactory httpClientFactory,
            IIocNodeService iocService,
            IIocNodeRepository iocRepo,
            ILogger<DataFeedService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _iocService = iocService;
            _iocRepo = iocRepo;
            _logger = logger;
        }

        // Hàm băm thuật toán để sinh Key cố định (Deterministic Key)
        private string GenerateDeterministicKey(string input)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(input.ToLowerInvariant().Trim());
                var hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        private int CalculateDynamicRiskScore(OtxPulse pulse, OtxIndicator indicator)
        {
            int score = 0;
            if (indicator.IsActive == 1) score += 20; else score += 5;
            if (indicator.Expiration.HasValue && indicator.Expiration.Value < DateTime.UtcNow) score -= 15;
            if (indicator.Observations > 100) score += 15;
            else if (indicator.Observations > 10) score += 10;
            else if (indicator.Observations > 0) score += 5;

            string roleStr = (indicator.Role ?? "").ToLower();
            if (roleStr.Contains("c2") || roleStr.Contains("malware") || roleStr.Contains("phishing") || roleStr.Contains("exploit")) score += 10;

            string typeStr = (indicator.Type ?? "").ToLower();
            if (typeStr.Contains("hash") || typeStr.Contains("md5") || typeStr.Contains("sha")) score += 10;
            else if (typeStr.Contains("domain") || typeStr.Contains("hostname")) score += 7;
            else if (typeStr.Contains("ipv4") || typeStr.Contains("ipv6")) score += 5;

            string tlp = (pulse.TLP ?? "").ToLower();
            if (tlp == "red") score += 20;
            else if (tlp == "amber") score += 15;
            else if (tlp == "green") score += 10;
            else if (tlp == "white") score += 5;

            if (!string.IsNullOrWhiteSpace(pulse.Adversary)) score += 15;
            if (pulse.TargetedCountries != null && pulse.TargetedCountries.Count > 0) score += 5;
            if (pulse.Industries != null && pulse.Industries.Count > 0) score += 5;

            if (pulse.References != null)
            {
                if (pulse.References.Count > 2) score += 10;
                else if (pulse.References.Count > 0) score += 5;
            }
            if (pulse.Revision > 5) score += 5;

            return Math.Max(1, Math.Min(100, score));
        }

        public async Task<int> SyncAlienVaultDataAsync()
        {
            var client = _httpClientFactory.CreateClient("AlienVaultClient");
            int totalAdded = 0;
            string currentUrl = "pulses/subscribed?limit=15"; // Đã đổi về 15 theo ý em

            try
            {
                while (!string.IsNullOrEmpty(currentUrl))
                {
                    var response = await client.GetFromJsonAsync<OtxPulseResponse>(currentUrl);
                    if (response?.Results == null || !response.Results.Any()) break;

                    // SỬ DỤNG DICTIONARY ĐỂ TỰ ĐỘNG LỌC TRÙNG DỮ LIỆU TRÊN RAM
                    var nodesDict = new Dictionary<string, IocNode>();
                    var edgesDict = new Dictionary<string, object>();

                    foreach (var pulse in response.Results)
                    {
                        string campaignIdStr = $"Campaign_{pulse.Id}";
                        string campaignKey = GenerateDeterministicKey(campaignIdStr);
                        string pulseName = string.IsNullOrWhiteSpace(pulse.Name) ? campaignIdStr : pulse.Name;

                        // 1. Thêm Campaign Node vào Dictionary (nếu trùng key tự đè)
                        nodesDict[campaignKey] = new IocNode
                        {
                            Key = campaignKey,
                            Type = "Campaign",
                            Value = pulseName,
                            RiskScore = 90,
                            OriginRef = "AlienVault OTX",
                            Tags = new List<string> { "Pulse" }
                        };

                        string? previousIocKey = null;
                        string? firstIocKey = null;

                        foreach (var ind in pulse.Indicators)
                        {
                            var mappedType = MapOtxTypeToSystemType(ind.Type);
                            if (mappedType == null) continue;

                            string cleanValue = ind.Indicator.Trim();
                            string iocKey = GenerateDeterministicKey(cleanValue);
                            int dynamicScore = CalculateDynamicRiskScore(pulse, ind);

                            // 2. Thêm IOC Node vào Dictionary
                            if (!nodesDict.ContainsKey(iocKey))
                            {
                                nodesDict[iocKey] = new IocNode
                                {
                                    Key = iocKey,
                                    Type = mappedType,
                                    Value = cleanValue,
                                    RiskScore = dynamicScore,
                                    OriginRef = "AlienVault OTX",
                                    Tags = new List<string> { "OTX", "AutoSync" }
                                };
                            }
                            else
                            {
                                // Nếu trùng (cùng 1 IP trong nhiều pulse), lấy RiskScore cao hơn
                                nodesDict[iocKey].RiskScore = Math.Max(nodesDict[iocKey].RiskScore, dynamicScore);
                            }

                            // 3. Thêm Edge Belongs_to (tạo chuỗi Hash làm Key lọc trùng cho Edge)
                            string belongsToEdgeKey = $"{iocKey}_belongs_to_{campaignKey}";
                            edgesDict[belongsToEdgeKey] = new
                            {
                                _from = $"IocNodes/{iocKey}",
                                _to = $"IocNodes/{campaignKey}",
                                RelationType = "belongs_to",
                                OriginRef = "AlienVault AutoSync"
                            };

                            // 4. Thêm Edge Related_ioc (Ring Topology)
                            if (!string.IsNullOrEmpty(previousIocKey) && iocKey != previousIocKey)
                            {
                                string relatedEdgeKey = $"{iocKey}_related_ioc_{previousIocKey}";
                                edgesDict[relatedEdgeKey] = new
                                {
                                    _from = $"IocNodes/{iocKey}",
                                    _to = $"IocNodes/{previousIocKey}",
                                    RelationType = "related_ioc"
                                };
                            }
                            if (firstIocKey == null) firstIocKey = iocKey;
                            previousIocKey = iocKey;

                            // KIỂM TRA CHUNKING (2000)
                            if (nodesDict.Count >= 2000)
                            {
                                // Chuyển Dictionary.Values thành List để truyền xuống DB
                                await _iocRepo.BulkUpsertNodesAsync(nodesDict.Values.ToList());
                                await _iocRepo.BulkInsertEdgesAsync(edgesDict.Values.ToList());
                                totalAdded += nodesDict.Count;

                                nodesDict.Clear();
                                edgesDict.Clear();
                            }
                        }

                        if (!string.IsNullOrEmpty(previousIocKey) && !string.IsNullOrEmpty(firstIocKey) && previousIocKey != firstIocKey)
                        {
                            string closeRingKey = $"{previousIocKey}_related_ioc_{firstIocKey}";
                            edgesDict[closeRingKey] = new { _from = $"IocNodes/{previousIocKey}", _to = $"IocNodes/{firstIocKey}", RelationType = "related_ioc" };
                        }
                    }

                    // Đẩy nốt phần dư trước khi qua trang kế
                    if (nodesDict.Any())
                    {
                        await _iocRepo.BulkUpsertNodesAsync(nodesDict.Values.ToList());
                        await _iocRepo.BulkInsertEdgesAsync(edgesDict.Values.ToList());
                        totalAdded += nodesDict.Count;
                    }

                    currentUrl = response.Next;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi cào dữ liệu: {ex.Message}");
            }

            _logger.LogInformation($"[Sync] Đã kéo và nối thành công {totalAdded} IOCs bằng Batch Processing.");
            return totalAdded;
        }

        private string? MapOtxTypeToSystemType(string otxType)
        {
            var type = otxType.ToLower();
            if (type == "ipv4" || type == "ipv6") return "IP";
            if (type == "domain" || type == "hostname") return "Domain";
            if (type.Contains("hash") || type == "md5" || type == "sha1" || type == "sha256") return "Hash";
            return null;
        }
    }
}