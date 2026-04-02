using System.Text.Json.Serialization;

namespace backend.Models
{
    public class User
    {
        [JsonPropertyName("_key")]
        public string? Key { get; set; } 

        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Analyst"; 
        public bool IsActive { get; set; } = true;
    }
}
