using System.Text.Json.Serialization;

namespace firstgptapp.Models
{
    public class Message
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}