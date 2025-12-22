using System.Text.Json.Serialization;

namespace firstgptapp.Models
{
    public class ChoiceStream
    {
        [JsonPropertyName("delta")]
        public DeltaStream Delta { get; set; }
        [JsonPropertyName("finish_reason")]
        public string Finish_Reason { get; set; }
        [JsonPropertyName("index")]
        public int Index { get; set; }
    }
}