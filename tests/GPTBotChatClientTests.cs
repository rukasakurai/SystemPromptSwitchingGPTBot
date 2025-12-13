using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using _07JP27.SystemPromptSwitchingGPTBot.Bots;
using _07JP27.SystemPromptSwitchingGPTBot.SystemPrompt;

namespace SystemPromptSwitchingGPTBot.Tests
{
    /// <summary>
    /// Tests for GPTBot chat client integration.
    /// 
    /// Note: AzureOpenAIClient and ChatClient in Azure.AI.OpenAI 2.x are sealed classes
    /// with internal constructors, making them impossible to mock with Moq.
    /// These tests verify the bot construction and configuration only.
    /// For full integration testing, use real Azure OpenAI credentials.
    /// </summary>
    public class GPTBotChatClientTests
    {
        [Fact]
        public void GPTBot_CanBeConstructed_WithValidDependencies()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["OpenAIDeployment"]).Returns("test-deployment");
            mockConfig.Setup(c => c["OpenAIEndpoint"]).Returns("https://test.openai.azure.com/");

            var conversationState = new ConversationState(new MemoryStorage());
            var userState = new UserState(new MemoryStorage());
            var systemPrompts = new List<IGptConfiguration> { new Default() };
            var mockLogger = new Mock<ILogger<GPTBot>>();

            // Act - Construct bot with null client (for unit testing purposes)
            var bot = new GPTBot(
                mockConfig.Object,
                null!, // AzureOpenAIClient cannot be mocked - use null for construction test
                conversationState,
                userState,
                systemPrompts,
                mockLogger.Object
            );

            // Assert
            Assert.NotNull(bot);
        }

        [Fact]
        public void SystemPrompts_ShouldBeConfigured()
        {
            // Arrange & Act
            var defaultPrompt = new Default();
            var translatePrompt = new Translate();

            // Assert
            Assert.NotNull(defaultPrompt.Id);
            Assert.NotNull(defaultPrompt.Command);
            Assert.NotNull(defaultPrompt.SystemPrompt);
            Assert.NotNull(translatePrompt.Id);
            Assert.NotNull(translatePrompt.Command);
            Assert.NotNull(translatePrompt.SystemPrompt);
        }
    }
}
