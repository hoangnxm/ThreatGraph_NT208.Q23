using backend.Models;
using backend.Repositories;
using IocNodes.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
        private int CalculateDynamicRiskScore(OtxPulse pulse, OtxIndicator indicator)
        {
            int score = 0;

            // 1. Trạng thái hoạt động
            if (indicator.IsActive == 1) score += 20;
            else score += 5;

            // Trừ điểm nếu đã quá hạn
            if (indicator.Expiration.HasValue && indicator.Expiration.Value < DateTime.UtcNow)
            {
                score -= 15;
            }

            // 2. Độ phổ biến (Observations)
            if (indicator.Observations > 100) score += 15;
            else if (indicator.Observations > 10) score += 10;
            else if (indicator.Observations > 0) score += 5;

            // 3. Vai trò (Role) & Phân loại (Type)
            string roleStr = (indicator.Role ?? "").ToLower();
            if (roleStr.Contains("c2") || roleStr.Contains("malware") || roleStr.Contains("phishing") || roleStr.Contains("exploit"))
                score += 10;

            // Type cũ của ông viết hoa hay thường thì cứ dùng thuộc tính của ông nhé
            string typeStr = (indicator.Type ?? "").ToLower();
            if (typeStr.Contains("hash") || typeStr.Contains("md5") || typeStr.Contains("sha")) score += 10;
            else if (typeStr.Contains("domain") || typeStr.Contains("hostname")) score += 7;
            else if (typeStr.Contains("ipv4") || typeStr.Contains("ipv6")) score += 5;

            // 4. Mức độ nhạy cảm (TLP)
            string tlp = (pulse.TLP ?? "").ToLower();
            if (tlp == "red") score += 20;
            else if (tlp == "amber") score += 15;
            else if (tlp == "green") score += 10;
            else if (tlp == "white") score += 5;

            // 5. Mức độ tinh vi (Adversary - Nhóm APT)
            if (!string.IsNullOrWhiteSpace(pulse.Adversary)) score += 15;

            // 6. Nhắm mục tiêu cụ thể
            if (pulse.TargetedCountries != null && pulse.TargetedCountries.Count > 0) score += 5;
            if (pulse.Industries != null && pulse.Industries.Count > 0) score += 5;
            // 7. Độ uy tín và Cập nhật
            if (pulse.References != null)
            {
                if (pulse.References.Count > 2) score += 10;
                else if (pulse.References.Count > 0) score += 5;
            }
            if (pulse.Revision > 5) score += 5;

            // Cân bằng điểm
            return Math.Max(1, Math.Min(100, score));
        }

        public async Task<int> SyncAlienVaultDataAsync()
        {
            var client = _httpClientFactory.CreateClient("AlienVaultClient");
            int addedCount = 0;

            try
            {
                // Quay về dùng subscribed, tăng limit lên 10 để kéo cho sướng
                var response = await client.GetFromJsonAsync<OtxPulseResponse>("pulses/subscribed?limit=10");
                if (response?.Results == null) return 0;

                foreach (var pulse in response.Results)
                {
                    // --- BƯỚC 1: TẠO NODE CAMPAIGN CHA ---
                    string campaignKey = string.Empty;
                    string pulseName = string.IsNullOrWhiteSpace(pulse.Name) ? $"Campaign_{pulse.Id}" : pulse.Name;

                    var existingCamp = await _iocRepo.GetByValueAsync(pulseName);

                    if (existingCamp != null && !string.IsNullOrEmpty(existingCamp.Key))
                    {
                        campaignKey = existingCamp.Key;
                    }
                    else
                    {
                        var campReq = new CreateIocNodeRequest
                        {
                            Type = "Campaign",
                            Value = pulseName,
                            RiskScore = 90,
                            OriginRef = "AlienVault OTX",
                            Tags = new List<string> { "Pulse" }
                        };
                        try
                        {
                            var createdCamp = await _iocService.CreateAsync(campReq);
                            campaignKey = createdCamp.Id;
                        }
                        catch { continue; }
                    }

                    if (string.IsNullOrEmpty(campaignKey)) continue;

                    // --- BƯỚC 2: QUÉT TRỰC TIẾP INDICATORS TỪ PULSE ---
                    // (Không cần phải gọi API móc ruột nữa vì nó có sẵn rồi)
                    string? previousIocKey = null;
                    string? firstIocKey = null;

                    foreach (var ind in pulse.Indicators.Take(100))
                    {
                        // Gọi hàm map của ông (Lưu ý: tùy cách ông viết DTO, Type có thể là Type hoặc type)
                        var mappedType = MapOtxTypeToSystemType(ind.Type);
                        if (mappedType == null) continue;

                        string cleanValue = ind.Indicator.Trim();
                        string iocKey = string.Empty;

                        // ==========================================
                        // GỌI HÀM TÍNH ĐIỂM Ở NGAY ĐÂY
                        // ==========================================
                        int dynamicScore = CalculateDynamicRiskScore(pulse, ind);

                        var existingIoc = await _iocRepo.GetByValueAsync(cleanValue);

                        if (existingIoc != null && !string.IsNullOrEmpty(existingIoc.Key))
                        {
                            iocKey = existingIoc.Key;
                            // (Lưu ý: Nếu node đã tồn tại, code hiện tại của ông chỉ lấy Key chứ chưa update điểm mới. 
                            // Tạm thời cứ giữ nguyên thế này để an toàn, không phá luồng cũ).
                        }
                        else
                        {
                            var iocReq = new CreateIocNodeRequest
                            {
                                Type = mappedType,
                                Value = cleanValue,
                                // THAY VÌ 70, GÁN BIẾN ĐÃ TÍNH VÀO ĐÂY
                                RiskScore = dynamicScore,
                                OriginRef = "AlienVault OTX",
                                Tags = new List<string> { "OTX", "AutoSync" }
                            };
                            try
                            {
                                var createdIoc = await _iocService.CreateAsync(iocReq);
                                iocKey = createdIoc.Id;
                            }
                            catch { continue; }
                        }

                        // Nối đồ thị
                        if (!string.IsNullOrEmpty(iocKey))
                        {
                            try
                            {
                                await _iocRepo.CreateRelationshipAsync(
                                    fromKey: iocKey,
                                    toKey: campaignKey,
                                    relationType: "belongs_to",
                                    originRef: "AlienVault AutoSync"
                                );
                                addedCount++;
                            }
                            catch { } // Lỗi trùng lặp relationship thì bỏ qua

                            if (!string.IsNullOrEmpty(previousIocKey) && iocKey != previousIocKey)
                            {
                                try
                                {
                                    await _iocRepo.CreateRelationshipAsync(iocKey, previousIocKey, "related_ioc", "AlienVault Ring Topology");
                                }
                                catch { }
                            }

                            if (firstIocKey == null) firstIocKey = iocKey;

                            previousIocKey = iocKey;
                        }
                    }

                    if (!string.IsNullOrEmpty(previousIocKey) && !string.IsNullOrEmpty(firstIocKey) && previousIocKey != firstIocKey)
                    {
                        try
                        {
                            await _iocRepo.CreateRelationshipAsync(previousIocKey, firstIocKey, "related_ioc", "AlienVault Ring Topology");
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi cào dữ liệu: {ex.Message}");
            }

            _logger.LogInformation($"[Sync] Đã kéo và nối thành công {addedCount} IOCs mới.");
            return addedCount;
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
