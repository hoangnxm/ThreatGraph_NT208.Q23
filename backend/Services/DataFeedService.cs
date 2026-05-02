using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using IocNodes.DTOs;
using backend.Repositories;
using Microsoft.Extensions.Logging;

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
                var response = await client.GetFromJsonAsync<OtxPulseResponse>("pulses/subscribed?limit=2");
                if (response?.Results == null) return 0;

                foreach (var pulse in response.Results)
                {
                    // --- BƯỚC 1: XỬ LÝ NODE CHIẾN DỊCH (NODE CHA) ---
                    string campaignKey = string.Empty;
                    var existingCamp = await _iocRepo.GetByValueAsync(pulse.Name);

                    if (existingCamp != null && !string.IsNullOrEmpty(existingCamp.Key)) 
                    {
                        campaignKey = existingCamp.Key; 
                    } 
                    else 
                    {
                        var campReq = new CreateIocNodeRequest {
                            Type = "Campaign", 
                            Value = pulse.Name, 
                            RiskScore = 100, 
                            OriginRef = "AlienVault OTX", 
                            Tags = new List<string> { "Pulse", "Campaign" }
                        };
                        try {
                            var createdCamp = await _iocService.CreateAsync(campReq);
                            campaignKey = createdCamp.Id; 
                        } catch { continue; } 
                    }

                    // --- BƯỚC 2: XỬ LÝ NODE IOC (NODE CON) ---
                    foreach (var indicator in pulse.Indicators)
                    {
                        var type = MapOtxTypeToSystemType(indicator.Type);
                        if (type == null) continue;

                        string iocKey = string.Empty;
                        string cleanValue = indicator.Indicator.Trim(); 
                        var existingIoc = await _iocRepo.GetByValueAsync(cleanValue);

                        if (existingIoc != null && !string.IsNullOrEmpty(existingIoc.Key)) 
                        {
                            iocKey = existingIoc.Key;
                            var updateReq = new UpdateIocNodeRequest {
                                RiskScore = 90, 
                                Country = existingIoc.Country, 
                                Tags = existingIoc.Tags ?? new List<string>(), 
                                OriginRef = $"AlienVault OTX - {pulse.Name}"
                            };
                            if (!updateReq.Tags.Contains(pulse.Name)) updateReq.Tags.Add(pulse.Name);
                            await _iocService.UpdateAsync(iocKey, updateReq);
                        } 
                        else 
                        {
                            var iocReq = new CreateIocNodeRequest {
                                Type = type, 
                                Value = cleanValue, 
                                RiskScore = 80, 
                                OriginRef = $"AlienVault OTX - {pulse.Name}", 
                                Tags = new List<string> { "OTX", "AutoSync" }
                            };
                            try {
                                var createdIoc = await _iocService.CreateAsync(iocReq);
                                iocKey = createdIoc.Id;
                                addedCount++;
                            } catch { continue; }
                        }

                        if (!string.IsNullOrEmpty(iocKey) && !string.IsNullOrEmpty(campaignKey))
                        {
                            try
                            {
                                await _iocRepo.CreateRelationshipAsync(
                                    fromKey: iocKey,
                                    toKey: campaignKey,
                                    relationType: "belongs_to",
                                    originRef: "AlienVault AutoSync"
                                );
                                _logger.LogInformation($"[Graph] Nối THÀNH CÔNG: {iocKey} -> {campaignKey}");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError($"[Graph] Lỗi kết nối: {ex.Message}");
                            }
                        }
                        else
                        {
                            // Camera chớp đỏ nếu C# lại bị mù ID
                            _logger.LogWarning($"[Graph] LỖI ID RỖNG! iocKey: '{iocKey}', campaignKey: '{campaignKey}'");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi cào dữ liệu: {ex.Message}");
            }
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