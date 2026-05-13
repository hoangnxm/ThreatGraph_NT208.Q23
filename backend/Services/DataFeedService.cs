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
            string currentUrl = "pulses/subscribed?limit=20"; // Limit vừa phải để không nghẽn mạng

            try
            {
                while (!string.IsNullOrEmpty(currentUrl))
                {
                    var response = await client.GetFromJsonAsync<OtxPulseResponse>(currentUrl);
                    if (response?.Results == null || !response.Results.Any()) break;

                    var nodesToUpsert = new List<IocNode>();
                    var edgesToInsert = new List<dynamic>();

                    foreach (var pulse in response.Results)
                    {
                        string campaignIdStr = $"Campaign_{pulse.Id}";
                        string campaignKey = GenerateDeterministicKey(campaignIdStr);
                        string pulseName = string.IsNullOrWhiteSpace(pulse.Name) ? campaignIdStr : pulse.Name;

                        nodesToUpsert.Add(new IocNode
                        {
                            Key = campaignKey,
                            Type = "Campaign",
                            Value = pulseName,
                            RiskScore = 90,
                            OriginRef = "AlienVault OTX",
                            Tags = new List<string> { "Pulse" }
                        });

                        string? previousIocKey = null;
                        string? firstIocKey = null;

                        foreach (var ind in pulse.Indicators)
                        {
                            var mappedType = MapOtxTypeToSystemType(ind.Type);
                            if (mappedType == null) continue;

                            string cleanValue = ind.Indicator.Trim();
                            string iocKey = GenerateDeterministicKey(cleanValue);
                            int dynamicScore = CalculateDynamicRiskScore(pulse, ind);

                            nodesToUpsert.Add(new IocNode
                            {
                                Key = iocKey,
                                Type = mappedType,
                                Value = cleanValue,
                                RiskScore = dynamicScore,
                                OriginRef = "AlienVault OTX",
                                Tags = new List<string> { "OTX", "AutoSync" }
                            });

                            edgesToInsert.Add(new
                            {
                                _from = $"IocNodes/{iocKey}",
                                _to = $"IocNodes/{campaignKey}",
                                RelationType = "belongs_to",
                                OriginRef = "AlienVault AutoSync"
                            });

                            if (!string.IsNullOrEmpty(previousIocKey) && iocKey != previousIocKey)
                            {
                                edgesToInsert.Add(new { _from = $"IocNodes/{iocKey}", _to = $"IocNodes/{previousIocKey}", RelationType = "related_ioc" });
                            }
                            if (firstIocKey == null) firstIocKey = iocKey;
                            previousIocKey = iocKey;

                            // Đẩy xuống DB mỗi khi gom đủ 2000 dòng để giải phóng RAM
                            if (nodesToUpsert.Count >= 2000)
                            {
                                await _iocRepo.BulkUpsertNodesAsync(nodesToUpsert);
                                await _iocRepo.BulkInsertEdgesAsync(edgesToInsert);
                                totalAdded += nodesToUpsert.Count;
                                nodesToUpsert.Clear();
                                edgesToInsert.Clear();
                            }
                        }

                        if (!string.IsNullOrEmpty(previousIocKey) && !string.IsNullOrEmpty(firstIocKey) && previousIocKey != firstIocKey)
                        {
                            edgesToInsert.Add(new { _from = $"IocNodes/{previousIocKey}", _to = $"IocNodes/{firstIocKey}", RelationType = "related_ioc" });
                        }
                    }

                    // Đẩy nốt phần dư trước khi qua trang kế
                    if (nodesToUpsert.Any())
                    {
                        await _iocRepo.BulkUpsertNodesAsync(nodesToUpsert);
                        await _iocRepo.BulkInsertEdgesAsync(edgesToInsert);
                        totalAdded += nodesToUpsert.Count;
                    }

                    // Gán URL trang tiếp theo để lặp
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