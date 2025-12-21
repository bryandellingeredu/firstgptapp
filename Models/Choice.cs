using System.Text.Json.Serialization;

namespace firstgptapp.Models
{
    public class Choice
    {
        [JsonPropertyName("message")]
        public Message Message { get; set; }

        [JsonPropertyName("finish_reason")]
        public string FinishReason { get; set; }

        [JsonPropertyName("index")]
        public int Index { get; set; }
    }
}