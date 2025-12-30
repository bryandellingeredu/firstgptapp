using firstgptapp.Interfaces;
using System.Text.Json;

namespace firstgptapp.Tools
{
    public class MyFriendsBirthdayToolHandler : IToolHandler
    {
        public string Name => "GetMyFriendsBirthday";
        public string Description => "Get the list of my friends whose birthday are the same as the given date.";

        public BinaryData GetParametersSchema() => BinaryData.FromBytes("""
        {
            "type": "object",
            "properties": {
                "birthday": {
                    "type": "string",
                    "description": "A MM-DD date string."
                }
            },
            "required": [ "birthday" ]
        }
        """u8.ToArray());

        public Task<string> InvokeAsync(JsonElement parameters)
        {
            if (!parameters.TryGetProperty("birthday", out var birthdayProp))
                return Task.FromResult("Missing 'birthday' parameter.");
            string currentDate = birthdayProp.GetString();

            if (string.IsNullOrWhiteSpace(currentDate))
                return Task.FromResult("Missing 'currentDate' parameter.");

            if (!DateTime.TryParse(currentDate, out var date))
                return Task.FromResult("Invalid date format. Please use yyyy-MM-dd.");

            string key = date.ToString("MM-dd");

            var birthdayMap = new Dictionary<string, List<string>>
        {
            { "01-01", new List<string> { "Kay" } },
            { "02-14", new List<string> { "Max" } },
            { "03-03", new List<string> { "Randy" } },
            { "03-10", new List<string> { "Lucy", "Nina" } },
            { "05-31", new List<string> { "Alice", "Bob" } },
            { "06-01", new List<string> { "Cathy" } },
            { "06-10", new List<string> { "David", "Emma" } },
            { "07-15", new List<string> { "Tom" } },
            { "08-02", new List<string> { "Claire", "John", "Alex" } },
            { "08-20", new List<string> { "Judy", "May" } },
            { "09-05", new List<string> { "Nina" } },
            { "10-10", new List<string> { "Oscar", "Paul" } },
            { "11-25", new List<string> { "Quinn" } },
            { "12-31", new List<string> { "Rita", "Sam" } }
        };

            if (birthdayMap.TryGetValue(key, out var names))
            {
                string nameList = string.Join(", ", names);
                return Task.FromResult($"On {date:MMMM d}, it's the birthday of: {nameList}.");
            }
            return Task.FromResult($"There are no known birthdays on {date:MMMM d}.");
        }
    }
}
