using System.Text.Json.Serialization;

namespace firstgptapp.Models
{
    public class DeltaStream
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }
        [JsonPropertyName("content")]
        public string? Content { get; set; }
        [JsonPropertyName("refusal")]
        public string? Refusal { get; set; }
    }
}