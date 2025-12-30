

using firstgptapp.Interfaces;
using firstgptapp.Services.HelperClasses;
using firstgptapp.Tools;
using OpenAI.Chat;
using System.ClientModel;
using System.Text;
using System.Text.Json;
namespace firstgptapp.Services
{
    public class GPT3LibraryService
    {
        private readonly ChatClient _client;
        private readonly ToolRegistry toolRegistry;
        readonly IConfigurationRoot configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
        public GPT3LibraryService(string gptModel = "gpt-3.5-turbo")
        {
            _client = new ChatClient(model: gptModel, apiKey: configuration["OpenAI_API_Key"]);

            toolRegistry = new ToolRegistry(new List<IToolHandler>
                {
                    new CurrentDateTimeToolHandler(),
                    new MyFriendsBirthdayToolHandler()
                });
        }

        public ChatCompletion GetResponse(string prompt)
        {
            ChatCompletionOptions options = new();

            foreach (ChatTool tool in toolRegistry.GetAllTools())
            {
                options.Tools.Add(tool);
            }

            List<ChatMessage> messages = [
                ChatMessage.CreateSystemMessage("Your name is Jason. You are a helpful assistant."),
                ChatMessage.CreateUserMessage(prompt)
                ];

            ChatCompletion result;
            bool requiresAction;

            do
            {
                requiresAction = false;
                result = _client.CompleteChat(messages, options);
                switch (result.FinishReason)
                {
                    case ChatFinishReason.Stop:
                    case ChatFinishReason.Length:
                        messages.Add(ChatMessage.CreateAssistantMessage(result));
                        break;

                    case ChatFinishReason.ToolCalls:
                        messages.Add(new AssistantChatMessage(result));
                        foreach (var toolCall in result.ToolCalls)
                        {
                            using JsonDocument doc = JsonDocument.Parse(toolCall.FunctionArguments);
                            string toolResult = toolRegistry.InvokeAsync(toolCall.FunctionName, doc.RootElement).GetAwaiter().GetResult();
                            messages.Add(new ToolChatMessage(toolCall.Id, toolResult));
                        }
                        requiresAction = true;
                        break;

                    default:
                        break;
                }
            } while (requiresAction);

            return result;

        }

        public async Task<ChatCompletion> GetResponseAsync(string prompt)
        {
            ChatCompletion result = await _client.CompleteChatAsync(prompt);
            return result;
        }

        public CollectionResult<StreamingChatCompletionUpdate> GetStreamingResponse(string prompt)
        {
            List<ChatMessage> messages = [
                ChatMessage.CreateUserMessage(prompt)
                ];
            CollectionResult<StreamingChatCompletionUpdate> completionUpdates =
                _client.CompleteChatStreaming(messages);
            return completionUpdates;
        }

        public async IAsyncEnumerable<StreamingChatCompletionUpdate> GetStreamingResponseAsync(string prompt)
        {
            ChatCompletionOptions options = new();
            foreach (var tool in toolRegistry.GetAllTools())
            {
                options.Tools.Add(tool);
            }
            options.MaxOutputTokenCount = 500;

            List<ChatMessage> messages = [
                ChatMessage.CreateSystemMessage("Your name is Jason. You are a helpful assistant."),
                ChatMessage.CreateUserMessage(prompt)
                ];
            AsyncCollectionResult<StreamingChatCompletionUpdate> completionUpdates;
            bool requiresAction;

            do
            {
                requiresAction = false;
                completionUpdates = _client.CompleteChatStreamingAsync(messages, options);

                StringBuilder contentBuilder = new();
                StreamingChatToolCallsBuilder toolCallsBuilder = new();

                await foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
                {
                    foreach (ChatMessageContentPart contentPart in completionUpdate.ContentUpdate)
                    {
                        contentBuilder.Append(contentPart.Text);
                    }

                    foreach (StreamingChatToolCallUpdate toolCallUpdate in completionUpdate.ToolCallUpdates)
                    {
                        toolCallsBuilder.Append(toolCallUpdate);
                    }

                    switch (completionUpdate.FinishReason)
                    {
                        case ChatFinishReason.ToolCalls:
                            {
                                // First, collect the accumulated function arguments into complete tool calls to be processed
                                IReadOnlyList<ChatToolCall> toolCalls = toolCallsBuilder.Build();

                                // Next, add the assistant message with tool calls to the conversation history.
                                AssistantChatMessage assistantMessage = new(toolCalls);

                                if (contentBuilder.Length > 0)
                                {
                                    assistantMessage.Content.Add(ChatMessageContentPart.CreateTextPart(contentBuilder.ToString()));
                                }

                                messages.Add(assistantMessage);

                                // Then, add new tool message for each tool call to be resolved.
                                foreach (ChatToolCall toolCall in toolCalls)
                                {
                                    using JsonDocument argumentsJson = JsonDocument.Parse(toolCall.FunctionArguments);
                                    string toolResult = await toolRegistry.InvokeAsync(toolCall.FunctionName, argumentsJson.RootElement);
                                    messages.Add(new ToolChatMessage(toolCall.Id, toolResult));
                                }
                                requiresAction = true;
                                break;
                            }
                        case ChatFinishReason.Stop:
                        case ChatFinishReason.Length:
                        default:
                            {
                                yield return completionUpdate;
                                break;
                            }
                    }
                }
            } while (requiresAction);

        }
      
    }
}
