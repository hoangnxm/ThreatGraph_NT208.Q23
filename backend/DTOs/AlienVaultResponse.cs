using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IocNodes.DTOs
{
    public class OtxPulseResponse
    {
        // THÊM DÒNG NÀY ĐỂ CHẠY ĐƯỢC VÒNG LẶP WHILE
        [JsonPropertyName("next")]
        public string? Next { get; set; }

        public List<OtxPulse> Results { get; set; } = new();
    }

    public class OtxPulse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<OtxIndicator> Indicators { get; set; } = new();

        public string TLP { get; set; } = string.Empty;
        public string Adversary { get; set; } = string.Empty;
        public int Revision { get; set; }
        public List<string> TargetedCountries { get; set; } = new();
        public List<string> Industries { get; set; } = new();
        public List<string> References { get; set; } = new();
    }

    public class OtxIndicatorResponse
    {
        public List<OtxIndicator> Results { get; set; } = new();
    }

    public class OtxIndicator
    {
        public string Indicator { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public int? IsActive { get; set; }
        public int Observations { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime? Expiration { get; set; }
    }
}