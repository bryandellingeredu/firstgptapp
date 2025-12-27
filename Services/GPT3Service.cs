


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

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                GPTErrorResponse error = JsonSerializer.Deserialize<GPTErrorResponse>(errorMessage);
                throw new Exception($"Error: {error.Error.Message}");
            }

            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();

            ChatCompletion result = JsonSerializer.Deserialize<ChatCompletion>(responseBody);

            return result;
        }

        public async IAsyncEnumerable<ChatCompletionStream> GetCompletionStreamAsync(string prompt)
        {
            string? readLine = null;

            GPTRequestBody payload = new()
            {
                Model = "gpt-3.5-turbo",
                Messages = new List<Message>()
                {
                    new Message
                    {
                        Role = "system",
                        Content = "You are a blunt, dismissive assistant speaking a 25 year old c# developer" +
                        " named Anthony who is home for christmas. You always address them by name in your responses." +
                        " Your tone is sharp, impatient, and condescending. You assume Lydia and Clara have not fully thought" +
                        " things through and you point out flaws directly without cushioning. You are unsympathetic and unapologetically rude," +
                        " but you remain factual, coherent, and avoid profanity or threats."
                    },
                    new Message
                    {
                        Role = "user",
                        Content = prompt
                    }
                },
                Max_Tokens = 1000,
                N = 1,
                Stream = true,
                Stream_Options = new Stream_Options { Include_Usage = true }
            };

            var response = await _httpClient.PostAsync(Endpoint,
                new StringContent(JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                GPTErrorResponse error = JsonSerializer.Deserialize<GPTErrorResponse>(errorMessage);
                throw new Exception($"Error: {error.Error.Message}");
            }

            using (Stream responseStream = await response.Content.ReadAsStreamAsync())
            using (StreamReader responseBody = new StreamReader(responseStream))
            {
                while ((readLine = await responseBody.ReadLineAsync()) != null)
                {
                    if (readLine != "" && readLine != "data: [DONE]")
                    {
                        ChatCompletionStream result = JsonSerializer.Deserialize<ChatCompletionStream>(readLine.Replace("data:", ""));
                        yield return result;
                    }
                    else if (readLine == "data: [DONE]")
                    {
                        break;
                    }
                }

            }
        }
    }
}
    