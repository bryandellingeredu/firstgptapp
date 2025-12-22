using System.Text.Json.Serialization;

namespace firstgptapp.Models
{
    public class GPTErrorResponse
    {
        [JsonPropertyName("error")]
        public GPTError Error { get; set; }
    }
}
