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
                        var mappedType = MapOtxTypeToSystemType(ind.Type);
                        if (mappedType == null) continue;

                        string cleanValue = ind.Indicator.Trim();
                        string iocKey = string.Empty;

                        var existingIoc = await _iocRepo.GetByValueAsync(cleanValue);

                        if (existingIoc != null && !string.IsNullOrEmpty(existingIoc.Key))
                        {
                            iocKey = existingIoc.Key;
                        }
                        else
                        {
                            var iocReq = new CreateIocNodeRequest
                            {
                                Type = mappedType,
                                Value = cleanValue,
                                RiskScore = 70,
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
