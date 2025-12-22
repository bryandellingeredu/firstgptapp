using System.Text.Json.Serialization;

namespace firstgptapp.Models
{
    public class GPTRequestBody
    {
        [JsonPropertyName("model")] 
        public string Model { get; set; }

        [JsonPropertyName("messages")]
        public List<Message> Messages { get; set; }

        [JsonPropertyName("max_tokens")]
        public float? Max_Tokens { get; set; }

        [JsonPropertyName("n")]
        public int? N { get; set; }
        [JsonPropertyName("stream")]
        public bool? Stream { get; set; }
        [JsonPropertyName("stream_options")]
        public Stream_Options? Stream_Options { get; set; }
    }
}
