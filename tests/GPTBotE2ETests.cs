using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _07JP27.SystemPromptSwitchingGPTBot.Bots;
using _07JP27.SystemPromptSwitchingGPTBot.SystemPrompt;
using Azure.AI.OpenAI;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace SystemPromptSwitchingGPTBot.Tests
{
    /// <summary>
    /// End-to-end tests for GPTBot functionality.
    /// 
    /// Note: The Azure.AI.OpenAI 2.x SDK uses sealed classes and internal constructors
    /// for AzureOpenAIClient and ChatClient, making them difficult to mock directly.
    /// These tests focus on command handling logic which doesn't require OpenAI calls.
    /// For full integration tests, use the GPTBotChatClientTests with real credentials.
    /// </summary>
    public class GPTBotE2ETests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly ConversationState _conversationState;
        private readonly UserState _userState;
        private readonly List<IGptConfiguration> _systemPrompts;
        private readonly Mock<ILogger<GPTBot>> _mockLogger;
        private readonly MemoryStorage _storage;

        public GPTBotE2ETests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.Setup(c => c["OpenAIDeployment"]).Returns("test-deployment");
            _mockConfiguration.Setup(c => c["OpenAIEndpoint"]).Returns("https://test.openai.azure.com/");

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

        /// <summary>
        /// Creates a GPTBot with a null AzureOpenAIClient for testing command handling.
        /// Tests using this bot should only test command paths that don't call OpenAI.
        /// </summary>
        private GPTBot CreateBotForCommandTests()
        {
            // For command-only tests, we pass null and the bot will fail if OpenAI is called
            // This is intentional - these tests should only test command handling
            return new GPTBot(
                _mockConfiguration.Object,
                null!, // Will throw if OpenAI is actually called
                _conversationState,
                _userState,
                _systemPrompts,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task ModeSwitching_ShouldResetStateAndSwitchPrompt()
        {
            // Arrange
            var bot = CreateBotForCommandTests();
            var adapter = new TestAdapter();

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
        public async Task InvalidCommand_ShouldReturnNotFoundMessage()
        {
            // Arrange
            var bot = CreateBotForCommandTests();
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
        public async Task DefaultMode_ShouldSwitchSuccessfully()
        {
            // Arrange
            var bot = CreateBotForCommandTests();
            var adapter = new TestAdapter();

            // Act & Assert
            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                await bot.OnTurnAsync(turnContext, cancellationToken);
            })
            .Send("/default")
            .AssertReply(activity => {
                var text = activity.AsMessageActivity()?.Text;
                Assert.Contains("モードに設定しました", text);
            })
            .StartTestAsync();
        }

        [Fact]
        public async Task MultipleModeSwitches_ShouldWorkCorrectly()
        {
            // Arrange
            var bot = CreateBotForCommandTests();
            var adapter = new TestAdapter();

            // Act & Assert - Switch between modes
            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                await bot.OnTurnAsync(turnContext, cancellationToken);
            })
            .Send("/default")
            .AssertReply(activity => {
                var text = activity.AsMessageActivity()?.Text;
                Assert.Contains("モードに設定しました", text);
            })
            .Send("/translate")
            .AssertReply(activity => {
                var text = activity.AsMessageActivity()?.Text;
                Assert.Contains("翻訳", text);
            })
            .Send("/default")
            .AssertReply(activity => {
                var text = activity.AsMessageActivity()?.Text;
                Assert.Contains("モードに設定しました", text);
            })
            .StartTestAsync();
        }
    }
}
