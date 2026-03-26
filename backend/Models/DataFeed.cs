using System.Text.Json.Serialization;
namespace backend.Models
{
    public class DataFeed
    {
        [JsonPropertyName("_key")]
        public string? Key { get; set; }

        public string FeedName { get; set; } = string.Empty;
        public string ApiEndpoint { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public bool IsAutoFetch { get; set; } = false;
        public DateTime? LastFetchedAt { get; set; }
    }
}
