using System.Text.Json.Serialization;

namespace backend.Models
{
    public class IocRelationship
    {
        [JsonPropertyName("_key")]
        public string? Key { get; set; }

        [JsonPropertyName("_from")]
        public string From { get; set; } = string.Empty; 

        [JsonPropertyName("_to")]
        public string To { get; set; } = string.Empty;

        public string RelationType { get; set; } = string.Empty;
        public string OriginRef { get; set; } = string.Empty;
    }
}
