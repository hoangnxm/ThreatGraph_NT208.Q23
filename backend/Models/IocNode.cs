using Newtonsoft.Json; // BẮT BUỘC PHẢI LÀ THƯ VIỆN NÀY
using System;
using System.Collections.Generic;

namespace backend.Models;

public class IocNode
{
    // Dùng đúng [JsonProperty] để driver ArangoDB hiểu và map với _key
    [JsonProperty("_key", NullValueHandling = NullValueHandling.Ignore)]
    public string? Key { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public int RiskScore { get; set; } = 0;

    public string? Country { get; set; }

    public List<string> Tags { get; set; } = new List<string>();

    public string OriginRef { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}