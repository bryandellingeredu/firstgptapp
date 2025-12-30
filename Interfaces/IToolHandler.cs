using System.Text.Json;

namespace firstgptapp.Interfaces
{
    public interface IToolHandler
    {
        string Name { get; }
        string Description { get; }
        BinaryData GetParametersSchema();
        Task<string> InvokeAsync(JsonElement parameters);
    }
}
