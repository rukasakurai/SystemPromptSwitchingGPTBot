// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using _07JP27.SystemPromptSwitchingGPTBot.SystemPrompt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace _07JP27.SystemPromptSwitchingGPTBot.Bots
{
    public class GPTBot : ActivityHandler
    {
        private readonly IConfiguration _configuration;
        private readonly AzureOpenAIClient _azureOpenAIClient;
        private BotState _conversationState;
        private BotState _userState;
        private List<IGptConfiguration> _systemPrompts;

        private readonly ILogger<GPTBot> _logger;

        public GPTBot(IConfiguration configuration, AzureOpenAIClient azureOpenAIClient, ConversationState conversationState, UserState userState, List<IGptConfiguration> systemPrompts, ILogger<GPTBot> logger)
        {
            _configuration = configuration;
            _azureOpenAIClient = azureOpenAIClient;
            _conversationState = conversationState;
            _userState = userState;
            _systemPrompts = systemPrompts;
            _logger = logger;
        }
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var conversationStateAccessors = _conversationState.CreateProperty<ConversationData>(nameof(ConversationData));
            var conversationData = await conversationStateAccessors.GetAsync(turnContext, () => new ConversationData());
            var userStateAccessors = _userState.CreateProperty<UserProfile>(nameof(UserProfile));
            var userProfile = await userStateAccessors.GetAsync(turnContext, () => new UserProfile());

            string inputText = turnContext.Activity.Text;

            _logger.LogTrace("LogTrace");
            _logger.LogInformation("inputText: {inputText}", inputText);
            _logger.LogWarning("LogWarning");
            _logger.LogError("LogError");
            _logger.LogInformation("StackTrace: '{0}'", Environment.StackTrace);

            if (inputText.StartsWith("/"))
            {
                string command = inputText.Trim().ToLowerInvariant().Substring(1);

                if (command == "clear")
                {
                    var resetMessage = "会話履歴をクリアしました。";
                    await turnContext.SendActivityAsync(MessageFactory.Text(resetMessage, resetMessage), cancellationToken);
                    var currentMode = _systemPrompts.FirstOrDefault(x => x.Id == conversationData.CurrentConfigId);
                    conversationData.Messages = new() { new GptMessage() { Role = "system", Content = currentMode.SystemPrompt } };
                    return;
                }

                var systemPrompt = _systemPrompts.FirstOrDefault(x => x.Command == command);

                if (systemPrompt != null)
                {
                    // systemPromptのSystemPromptを返す
                    var switchedMessage = $"会話履歴をクリアして、**{systemPrompt.DisplayName}**モードに設定しました。\n\nこのモードでできること：{systemPrompt.Description}";
                    await turnContext.SendActivityAsync(MessageFactory.Text(switchedMessage, switchedMessage), cancellationToken);
                    conversationData.CurrentConfigId = systemPrompt.Id;
                    conversationData.Messages = new() { new GptMessage() { Role = "system", Content = systemPrompt.SystemPrompt } };
                    return;
                }
                else
                {
                    // systemPromptのCommandが一致しない場合は、ユーザーに通知する
                    string notFoundMessage = "指定されたコマンドが見つかりませんでした。";
                    await turnContext.SendActivityAsync(MessageFactory.Text(notFoundMessage, notFoundMessage), cancellationToken);
                    return;
                }
            }

            if (string.IsNullOrEmpty(userProfile.Name))
            {
                string userNameFronContext = turnContext.Activity.From.Name;
                userProfile.Name = userNameFronContext;
            }

            var currentConfing = _systemPrompts.FirstOrDefault(x => x.Id == (conversationData.CurrentConfigId != null ? conversationData.CurrentConfigId : "default"));

            if (String.IsNullOrEmpty(conversationData.CurrentConfigId))
            {
                conversationData.Messages = new() { new GptMessage() { Role = "system", Content = currentConfing.SystemPrompt } };
            }

            List<GptMessage> messages = new();
            if (conversationData.Messages?.Count > 0)
            {
                messages = conversationData.Messages;
            }

            messages.Add(new GptMessage() { Role = "user", Content = inputText });

            try
            {
                // TODO:会話履歴がトークン上限を超えないことを事前に確認して、超えるようなら直近n件のみ送るようにする
                ChatCompletion completion = await generateMessage(messages, currentConfing.Temperature, currentConfing.MaxTokens);

                if (completion == null || completion.Content == null || completion.Content.Count == 0)
                {
                    _logger.LogError("OpenAI API returned null or empty response");
                    await turnContext.SendActivityAsync(MessageFactory.Text("申し訳ございません。応答の生成中にエラーが発生しました。", "申し訳ございません。応答の生成中にエラーが発生しました。"), cancellationToken);
                    return;
                }

                // Concatenate all content parts (usually only one, but robust for future changes)
                var replyText = string.Join("", completion.Content.Select(part => part.Text));
                await turnContext.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);

                messages.Add(new GptMessage() { Role = "assistant", Content = replyText });

                conversationData.Timestamp = turnContext.Activity.Timestamp.ToString();
                conversationData.ChannelId = turnContext.Activity.ChannelId;
                conversationData.Messages = messages;
            }
            catch (Azure.RequestFailedException ex)
            {
                _logger.LogError(ex, "OpenAI API request failed: {Message}", ex.Message);
                var errorMessage = "申し訳ございません。OpenAI API への接続中にエラーが発生しました。";
                if (ex.Status == 401 || ex.Status == 403)
                {
                    errorMessage += " 認証情報を確認してください。";
                }
                await turnContext.SendActivityAsync(MessageFactory.Text(errorMessage, errorMessage), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating message: {Message}", ex.Message);
                await turnContext.SendActivityAsync(MessageFactory.Text("申し訳ございません。メッセージの生成中にエラーが発生しました。", "申し訳ございません。メッセージの生成中にエラーが発生しました。"), cancellationToken);
            }
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // State保存
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    var welcomeText = "こんにちは。";
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                }
            }
        }

        private async Task<ChatCompletion> generateMessage(List<GptMessage> messages, float temperature = 0.0f, int maxTokens = 500)
        {
            var chatClient = _azureOpenAIClient.GetChatClient(_configuration["OpenAIDeployment"]);
            var chatMessages = new List<ChatMessage>();
            foreach (var message in messages)
            {
                switch (message.Role)
                {
                    case "user":
                        chatMessages.Add(new UserChatMessage(message.Content));
                        break;
                    case "assistant":
                        chatMessages.Add(new AssistantChatMessage(message.Content));
                        break;
                    case "system":
                        chatMessages.Add(new SystemChatMessage(message.Content));
                        break;
                }
            }
            var options = new ChatCompletionOptions
            {
                Temperature = temperature,
                MaxOutputTokenCount = maxTokens
            };
            ChatCompletion completion = await chatClient.CompleteChatAsync(chatMessages, options);
            return completion;
        }
    }
}
