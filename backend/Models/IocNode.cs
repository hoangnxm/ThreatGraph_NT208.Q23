using System.Text.Json.Serialization;
namespace backend.Models;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace IocNodes.Models
{
    /// <summary>
    /// Represents an Indicator of Compromise node stored in ArangoDB.
    /// _key and CreatedAt are managed by the database / repository layer and are read-only externally.
    /// </summary>
    public class IocNode
    {
        /// <summary>ArangoDB document key (_key). Assigned on insert.</summary>
        [JsonProperty("_key")]
        public string? Key { get; set; }

        /// <summary>IOC type: "IP" | "Domain" | "Hash"</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>Normalised (lowercase, trimmed) indicator value.</summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>Risk score 0–100.</summary>
        public int RiskScore { get; set; } = 0;

        /// <summary>ISO-3166-1 alpha-2 country code, optional.</summary>
        public string? Country { get; set; }

        /// <summary>Arbitrary classification tags.</summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>Reference to the originating source / feed.</summary>
        public string OriginRef { get; set; } = string.Empty;

        /// <summary>UTC timestamp set on creation.</summary>
        public DateTime CreatedAt { get; set; }
    }
}