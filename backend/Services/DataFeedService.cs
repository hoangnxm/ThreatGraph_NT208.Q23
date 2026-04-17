using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using IocNodes.DTOs;
using Microsoft.Extensions.Logging;

namespace IocNodes.Services
{
    public class DataFeedService : IDataFeedService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IIocNodeService _iocService;
        private readonly ILogger<DataFeedService> _logger;

        public DataFeedService(
            IHttpClientFactory httpClientFactory,
            IIocNodeService iocService,
            ILogger<DataFeedService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _iocService = iocService;
            _logger = logger;
        }

        public async Task<int> SyncAlienVaultDataAsync()
        {
            // Lấy ra HttpClient đã được cấu hình sẵn BaseUrl và ApiKey trong Program.cs
            var client = _httpClientFactory.CreateClient("AlienVaultClient");
            int addedCount = 0;

            try
            {
                // Gọi API lấy các chiến dịch (Pulse) mày đang theo dõi, lấy tạm 5 cái mới nhất để test
                var response = await client.GetFromJsonAsync<OtxPulseResponse>("pulses/subscribed?limit=2");

                if (response?.Results == null) return 0;

                foreach (var pulse in response.Results)
                {
                    foreach (var indicator in pulse.Indicators)
                    {
                        // Map loại dữ liệu của OTX sang chuẩn của hệ thống mày
                        var type = MapOtxTypeToSystemType(indicator.Type);
                        if (type == null) continue; // Bỏ qua các loại rác không quan tâm

                        var request = new CreateIocNodeRequest
                        {
                            Type = type,
                            Value = indicator.Indicator,
                            RiskScore = 80, // Tạm mặc định 80 cho dữ liệu từ nguồn Threat Intel
                            Country = null, // Dữ liệu OTX thường không có country code chuẩn 2 kí tự
                            OriginRef = $"AlienVault OTX - {pulse.Name}",
                            Tags = new List<string> { "OTX", "AutoSync" }
                        };

                        try
                        {
                            // Đẩy vào database qua Service cũ
                            await _iocService.CreateAsync(request);
                            addedCount++;
                        }
                        catch (Exception ex)
                        {
                            // Nếu ArangoDB chửi vì trùng Key, nó sẽ văng xuống đây và bỏ qua nhẹ nhàng
                            _logger.LogWarning($"Bỏ qua IOC {indicator.Indicator}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi cào AlienVault: {ex.Message}");
            }

            return addedCount;
        }

        // Hàm "Phiên dịch" loại IOC của OTX sang chuẩn của mày
        private string? MapOtxTypeToSystemType(string otxType)
        {
            var type = otxType.ToLower();
            if (type == "ipv4" || type == "ipv6") return "IP";
            if (type == "domain" || type == "hostname") return "Domain";
            if (type.Contains("hash") || type == "md5" || type == "sha1" || type == "sha256") return "Hash";
            return null; // Trả về null với mấy loại như email, URL, v.v.
        }
    }
}