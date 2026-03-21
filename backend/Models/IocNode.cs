namespace backend.Models
{
    public class IocNode
    {
        [JsonPropertyName("_key")]
        public string? Key { get; set; }

        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int RiskScore { get; set; } = 0;
        public string Country { get; set; } = "Unknown";
        public List<string> Tags { get; set; } = new List<string>();
        public string OriginRef { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
