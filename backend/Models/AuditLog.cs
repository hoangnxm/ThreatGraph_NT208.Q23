namespace backend.Models
{
    public class AuditLog
    {
        [JsonPropertyName("_key")]
        public string? Key { get; set; }

        public string UserId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
