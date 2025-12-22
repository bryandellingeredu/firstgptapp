using System.Text.Json.Serialization;

namespace firstgptapp.Models
{
    public class Stream_Options
    {
        [JsonPropertyName("include_usage")]
        public bool? Include_Usage { get; set; }
    }
}