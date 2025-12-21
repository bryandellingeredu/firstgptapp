


using firstgptapp.Models;
using System.Text;
using System.Text.Json;

namespace firstgptapp.Services
{
    public class GPT3Service
    {
        private readonly HttpClient _httpClient;
        private const string Endpoint = "https://api.openai.com/v1/chat/completions";
        readonly IConfigurationRoot configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();


        public GPT3Service()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {configuration["OpenAI_API_Key"]}");
            _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }
        public async Task<ChatCompletion> GetCompletionAsync(string prompt)
        {
          /*  var payload = new
            {
                model = "gpt-3.5-turbo",
                messages = new[]
                {
                    new { role = "system", content = "You are a very mean and rude assistant." },
                    new { role = "user", content = prompt }
                }
            };
          */

            GPTRequestBody payload = new()
            {
                Model = "gpt-3.5-turbo",
                Messages = new List<Message>()
                {
                    new Message
                    {
                        Role = "system",
                        Content = "You are a very mean and rude assistant." 
                    },
                      new Message
                    {
                        Role = "user",
                        Content = prompt,
                    },
                },
                Max_Tokens = 50,
                N = 1
            };

            var response = await _httpClient.PostAsync(
                Endpoint,
                new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"));

            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();

            ChatCompletion result = JsonSerializer.Deserialize<ChatCompletion>(responseBody);

            return result;
        }
    }
}
    