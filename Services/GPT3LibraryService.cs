

using firstgptapp.Interfaces;
using OpenAI.Chat;
using System.Buffers;
using System.ClientModel;
using System.Diagnostics;
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



        #region [05] Helper Classes for GPTService
        /// <summary>
        /// This class is responsible for incrementally collecting streaming tool call 
        /// updates (StreamingChatToolCallUpdate) from OpenAI’s Chat API, and finally 
        /// assembling them into a list of complete ChatToolCall objects.
        /// </summary>
        public class StreamingChatToolCallsBuilder
        {
            private readonly Dictionary<int, string> _indexToToolCallId = [];
            private readonly Dictionary<int, string> _indexToFunctionName = [];
            private readonly Dictionary<int, SequenceBuilder<byte>> _indexToFunctionArguments = [];

            /// <summary>
            /// Appends each incoming update from the stream.
            /// </summary>
            public void Append(StreamingChatToolCallUpdate toolCallUpdate)
            {
                // Keep track of which tool call ID belongs to this update index.
                if (toolCallUpdate.ToolCallId != null)
                {
                    _indexToToolCallId[toolCallUpdate.Index] = toolCallUpdate.ToolCallId;
                }

                // Keep track of which function name belongs to this update index.
                if (toolCallUpdate.FunctionName != null)
                {
                    _indexToFunctionName[toolCallUpdate.Index] = toolCallUpdate.FunctionName;
                }

                // Keep track of which function arguments belong to this update index,
                // and accumulate the arguments as new updates arrive.
                if (toolCallUpdate.FunctionArgumentsUpdate != null && !toolCallUpdate.FunctionArgumentsUpdate.ToMemory().IsEmpty)
                {
                    if (!_indexToFunctionArguments.TryGetValue(toolCallUpdate.Index, out SequenceBuilder<byte> argumentsBuilder))
                    {
                        argumentsBuilder = new SequenceBuilder<byte>();
                        _indexToFunctionArguments[toolCallUpdate.Index] = argumentsBuilder;
                    }

                    argumentsBuilder.Append(toolCallUpdate.FunctionArgumentsUpdate);
                }
            }

            /// <summary>
            /// Assembles all accumulated fragments into a complete list of ChatToolCall instances.
            /// </summary>
            public IReadOnlyList<ChatToolCall> Build()
            {
                List<ChatToolCall> toolCalls = [];

                foreach ((int index, string toolCallId) in _indexToToolCallId)
                {
                    ReadOnlySequence<byte> sequence = _indexToFunctionArguments[index].Build();

                    ChatToolCall toolCall = ChatToolCall.CreateFunctionToolCall(
                        id: toolCallId,
                        functionName: _indexToFunctionName[index],
                        functionArguments: BinaryData.FromBytes(sequence.ToArray()));

                    toolCalls.Add(toolCall);
                }

                return toolCalls;
            }
        }

        /// <summary>
        /// A generic helper to accumulate memory fragments and efficiently build 
        /// a ReadOnlySequence<T> for byte-stream-like data.
        /// </summary>
        public class SequenceBuilder<T>
        {
            Segment _first;
            Segment _last;

            /// <summary>
            /// Appends a memory segment to the internal linked list structure.
            /// </summary>
            public void Append(ReadOnlyMemory<T> data)
            {
                if (_first == null)
                {
                    Debug.Assert(_last == null);
                    _first = new Segment(data);
                    _last = _first;
                }
                else
                {
                    _last = _last!.Append(data);
                }
            }

            /// <summary>
            /// Constructs and returns a ReadOnlySequence<T> made from the accumulated segments.
            /// </summary>
            public ReadOnlySequence<T> Build()
            {
                if (_first == null)
                {
                    Debug.Assert(_last == null);
                    return ReadOnlySequence<T>.Empty;
                }

                if (_first == _last)
                {
                    Debug.Assert(_first.Next == null);
                    return new ReadOnlySequence<T>(_first.Memory);
                }

                return new ReadOnlySequence<T>(_first, 0, _last!, _last!.Memory.Length);
            }

            /// <summary>
            /// A custom implementation of ReadOnlySequenceSegment<T>. 
            /// It holds one memory block and points to the next one, 
            /// allowing the entire sequence to be reconstructed as a stream.
            /// </summary>
            private sealed class Segment : ReadOnlySequenceSegment<T>
            {
                public Segment(ReadOnlyMemory<T> items) : this(items, 0)
                {
                }

                private Segment(ReadOnlyMemory<T> items, long runningIndex)
                {
                    Debug.Assert(runningIndex >= 0);
                    Memory = items;
                    RunningIndex = runningIndex;
                }

                public Segment Append(ReadOnlyMemory<T> items)
                {
                    long runningIndex;
                    checked { runningIndex = RunningIndex + Memory.Length; }
                    Segment segment = new(items, runningIndex);
                    Next = segment;
                    return segment;
                }
            }
        }
        #endregion
    }
}
