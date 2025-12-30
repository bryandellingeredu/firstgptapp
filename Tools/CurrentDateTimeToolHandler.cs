using firstgptapp.Interfaces;
using System.Text.Json;

namespace firstgptapp.Tools
{
    public class CurrentDateTimeToolHandler : IToolHandler
    {
        public string Name => "GetCurrentDateTime";
        public string Description => "Get the current date time in UTC format.";

        public BinaryData GetParametersSchema() => BinaryData.FromBytes("""
        {
            "type": "object",
            "properties": {},
            "required": []
        }
        """u8.ToArray());

        public Task<string> InvokeAsync(JsonElement parameters)
        {
            return Task.FromResult(DateTime.UtcNow.ToString());
        }
    }
}
