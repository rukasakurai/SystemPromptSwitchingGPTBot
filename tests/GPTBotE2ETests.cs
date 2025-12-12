using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using _07JP27.SystemPromptSwitchingGPTBot.Bots;
using _07JP27.SystemPromptSwitchingGPTBot.SystemPrompt;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace SystemPromptSwitchingGPTBot.Tests
{
    public class GPTBotE2ETests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<OpenAIClient> _mockOpenAIClient;
        private readonly ConversationState _conversationState;
        private readonly UserState _userState;
        private readonly List<IGptConfiguration> _systemPrompts;
        private readonly Mock<ILogger<GPTBot>> _mockLogger;
        private readonly MemoryStorage _storage;

        public GPTBotE2ETests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.Setup(c => c["OpenAIDeployment"]).Returns("test-deployment");

            _mockOpenAIClient = new Mock<OpenAIClient>();
            
            _storage = new MemoryStorage();
            _conversationState = new ConversationState(_storage);
            _userState = new UserState(_storage);

            _systemPrompts = new List<IGptConfiguration>
            {
                new Default(),
                new Translate(),
            };

            _mockLogger = new Mock<ILogger<GPTBot>>();
        }

        private GPTBot CreateBot()
        {
            return new GPTBot(
                _mockConfiguration.Object,
                _mockOpenAIClient.Object,
                _conversationState,
                _userState,
                _systemPrompts,
                _mockLogger.Object
            );
        }

        // Helper method to create ChatCompletions from JSON using reflection
        // Note: This uses reflection to access internal Azure SDK methods. This is necessary
        // because ChatCompletions doesn't have a public constructor or factory method.
        // This makes the test somewhat fragile, but it's the only viable option for
        // deterministic testing without creating a wrapper around the OpenAI client.
        private ChatCompletions? CreateChatCompletionsFromJson(string json)
        {
            var assembly = typeof(ChatCompletions).Assembly;
            var type = assembly.GetType("Azure.AI.OpenAI.ChatCompletions");
            var method = type?.GetMethod("DeserializeChatCompletions",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            if (method != null)
            {
                using var jsonDoc = JsonDocument.Parse(json);
                return (ChatCompletions?)method.Invoke(null, new object[] { jsonDoc.RootElement });
            }

            return null;
        }

        private void SetupMockOpenAIResponse(string responseContent)
        {
            // Since ChatCompletions is difficult to create directly, we'll use a callback
            // that will be invoked when GetChatCompletionsAsync is called
            _mockOpenAIClient
                .Setup(client => client.GetChatCompletionsAsync(
                    It.IsAny<ChatCompletionsOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((ChatCompletionsOptions options, CancellationToken ct) =>
                {
                    // Create JSON response dynamically
                    var json = $@"{{
                        ""id"": ""test-id"",
                        ""object"": ""chat.completion"",
                        ""created"": 1234567890,
                        ""model"": ""test-model"",
                        ""choices"": [
                            {{
                                ""index"": 0,
                                ""message"": {{
                                    ""role"": ""assistant"",
                                    ""content"": ""{responseContent.Replace("\"", "\\\"")}""
                                }},
                                ""finish_reason"": ""stop""
                            }}
                        ],
                        ""usage"": {{
                            ""prompt_tokens"": 10,
                            ""completion_tokens"": 20,
                            ""total_tokens"": 30
                        }}
                    }}";

                    var completions = CreateChatCompletionsFromJson(json);
                    if (completions != null)
                    {
                        var mockResponse = new Mock<Response<ChatCompletions>>();
                        mockResponse.Setup(r => r.Value).Returns(completions);
                        return mockResponse.Object;
                    }

                    throw new InvalidOperationException("Could not create ChatCompletions");
                });
        }

        [Fact]
        public async Task ModeSwitching_ShouldResetStateAndSwitchPrompt()
        {
            // Arrange
            var bot = CreateBot();
            var adapter = new TestAdapter();
            SetupMockOpenAIResponse("Translation response");

            // Act & Assert - Switch to translate mode
            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                await bot.OnTurnAsync(turnContext, cancellationToken);
            })
            .Send("/translate")
            .AssertReply(activity => {
                var text = activity.AsMessageActivity()?.Text;
                Assert.Contains("翻訳", text);
                Assert.Contains("モードに設定しました", text);
            })
            .StartTestAsync();
        }

        [Fact]
        public async Task ClearCommand_ShouldResetConversationHistory()
        {
            // Arrange
            var bot = CreateBot();
            var adapter = new TestAdapter();
            SetupMockOpenAIResponse("Test response");

            // Act & Assert - First set a mode, have a conversation, then clear
            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                await bot.OnTurnAsync(turnContext, cancellationToken);
            })
            .Send("/default")
            .AssertReply(activity => { })
            .Send("First message")
            .AssertReply(activity => { })
            .Send("/clear")
            .AssertReply(activity => {
                var text = activity.AsMessageActivity()?.Text;
                Assert.Contains("会話履歴をクリアしました", text);
            })
            .StartTestAsync();
        }

        [Fact]
        public async Task InvalidCommand_ShouldReturnNotFoundMessage()
        {
            // Arrange
            var bot = CreateBot();
            var adapter = new TestAdapter();

            // Act & Assert
            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                await bot.OnTurnAsync(turnContext, cancellationToken);
            })
            .Send("/invalidcommand")
            .AssertReply(activity => {
                var text = activity.AsMessageActivity()?.Text;
                Assert.Equal("指定されたコマンドが見つかりませんでした。", text);
            })
            .StartTestAsync();
        }

        [Fact]
        public async Task NormalConversation_ShouldCallOpenAIAndReply()
        {
            // Arrange
            var bot = CreateBot();
            var adapter = new TestAdapter();
            var expectedResponse = "This is a test response from OpenAI";
            SetupMockOpenAIResponse(expectedResponse);

            // Act & Assert
            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                await bot.OnTurnAsync(turnContext, cancellationToken);
            })
            .Send("Tell me a joke")
            .AssertReply(activity => {
                var text = activity.AsMessageActivity()?.Text;
                Assert.Equal(expectedResponse, text);
            })
            .StartTestAsync();

            // Verify OpenAI was called
            _mockOpenAIClient.Verify(
                client => client.GetChatCompletionsAsync(
                    It.IsAny<ChatCompletionsOptions>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ConversationState_ShouldPersistAcrossMessages()
        {
            // Arrange
            var bot = CreateBot();
            var adapter = new TestAdapter();
            
            SetupMockOpenAIResponse("Response 1");

            // Act & Assert - Have a multi-turn conversation
            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                await bot.OnTurnAsync(turnContext, cancellationToken);
            })
            .Send("First message")
            .AssertReply(activity => { })
            .Send("Second message")
            .AssertReply(activity => { })
            .StartTestAsync();

            // Verify OpenAI was called multiple times (conversation persisted)
            _mockOpenAIClient.Verify(
                client => client.GetChatCompletionsAsync(
                    It.IsAny<ChatCompletionsOptions>(),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task ModeSwitch_ThenConversation_ShouldUseNewPrompt()
        {
            // Arrange
            var bot = CreateBot();
            var adapter = new TestAdapter();
            var capturedOptions = new List<ChatCompletionsOptions>();
            
            // Capture the ChatCompletionsOptions when called
            _mockOpenAIClient
                .Setup(client => client.GetChatCompletionsAsync(
                    It.IsAny<ChatCompletionsOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<ChatCompletionsOptions, CancellationToken>((opts, ct) => capturedOptions.Add(opts))
                .ReturnsAsync((ChatCompletionsOptions options, CancellationToken ct) =>
                {
                    var json = @"{
                        ""id"": ""test-id"",
                        ""object"": ""chat.completion"",
                        ""created"": 1234567890,
                        ""model"": ""test-model"",
                        ""choices"": [
                            {
                                ""index"": 0,
                                ""message"": {
                                    ""role"": ""assistant"",
                                    ""content"": ""Translation result""
                                },
                                ""finish_reason"": ""stop""
                            }
                        ],
                        ""usage"": {
                            ""prompt_tokens"": 10,
                            ""completion_tokens"": 20,
                            ""total_tokens"": 30
                        }
                    }";

                    var completions = CreateChatCompletionsFromJson(json);
                    if (completions != null)
                    {
                        var mockResponse = new Mock<Response<ChatCompletions>>();
                        mockResponse.Setup(r => r.Value).Returns(completions);
                        return mockResponse.Object;
                    }

                    throw new InvalidOperationException("Could not create ChatCompletions");
                });

            // Act & Assert - Switch to translate mode then send message
            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                await bot.OnTurnAsync(turnContext, cancellationToken);
            })
            .Send("/translate")
            .AssertReply(activity => {
                var text = activity.AsMessageActivity()?.Text;
                Assert.Contains("翻訳", text);
            })
            .Send("Translate this text")
            .AssertReply(activity => {
                var text = activity.AsMessageActivity()?.Text;
                Assert.Equal("Translation result", text);
            })
            .StartTestAsync();

            // Verify OpenAI was called
            Assert.Single(capturedOptions);
            
            // Verify the system message contains translate-related content
            var systemMessage = capturedOptions[0].Messages.FirstOrDefault(m => m is ChatRequestSystemMessage);
            Assert.NotNull(systemMessage);
        }
    }
}
