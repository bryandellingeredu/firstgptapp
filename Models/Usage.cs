using System.Text.Json.Serialization;

namespace firstgptapp.Models
{
    public class Usage
    {
        [JsonPropertyName("prompt_tokens")]
        public int Prompt_tokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int Completion_tokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int Total_tokens { get; set; }


    }
}