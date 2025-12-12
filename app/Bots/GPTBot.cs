// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
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
        private readonly OpenAIClient _openAIClient;
        private BotState _conversationState;
        private BotState _userState;
        private List<IGptConfiguration> _systemPrompts;

        private readonly ILogger<GPTBot> _logger;

        public GPTBot(IConfiguration configuration, OpenAIClient openAIClient, ConversationState conversationState, UserState userState, List<IGptConfiguration> systemPrompts, ILogger<GPTBot> logger)
        {
            _configuration = configuration;
            _openAIClient = openAIClient;
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

            // Ensure we have a valid configuration
            if (currentConfing == null)
            {
                currentConfing = _systemPrompts.FirstOrDefault(x => x.Id == "default");
                if (currentConfing == null)
                {
                    var errorMessage = "システムプロンプトの設定が見つかりませんでした。";
                    await turnContext.SendActivityAsync(MessageFactory.Text(errorMessage, errorMessage), cancellationToken);
                    return;
                }
            }

            if (String.IsNullOrEmpty(conversationData.CurrentConfigId))
            {
                conversationData.CurrentConfigId = currentConfing.Id;
                conversationData.Messages = new() { new GptMessage() { Role = "system", Content = currentConfing.SystemPrompt } };
            }

            List<GptMessage> messages = new();
            if (conversationData.Messages?.Count > 0)
            {
                messages = conversationData.Messages;
            }

            messages.Add(new GptMessage() { Role = "user", Content = inputText });

            // TODO:会話履歴がトークン上限を超えないことを事前に確認して、超えるようなら直近n件のみ送るようにする
            try
            {
                ChatCompletions response = await generateMessage(messages, currentConfing.Temperature, currentConfing.MaxTokens);

                if (response == null || response.Choices == null || response.Choices.Count == 0)
                {
                    var errorMessage = "申し訳ございません。AIからの応答を取得できませんでした。";
                    await turnContext.SendActivityAsync(MessageFactory.Text(errorMessage, errorMessage), cancellationToken);
                    _logger.LogError("OpenAI response was null or had no choices");
                    return;
                }

                var replyText = response.Choices[0].Message.Content;
                if (string.IsNullOrEmpty(replyText))
                {
                    var errorMessage = "申し訳ございません。AIからの応答が空でした。";
                    await turnContext.SendActivityAsync(MessageFactory.Text(errorMessage, errorMessage), cancellationToken);
                    _logger.LogError("OpenAI response content was null or empty");
                    return;
                }

                await turnContext.SendActivityAsync(MessageFactory.Text(replyText, replyText), cancellationToken);

                messages.Add(new GptMessage() { Role = "assistant", Content = replyText });

                conversationData.Timestamp = turnContext.Activity.Timestamp.ToString();
                conversationData.ChannelId = turnContext.Activity.ChannelId;
                conversationData.Messages = messages;
            }
            catch (RequestFailedException ex)
            {
                var errorMessage = $"申し訳ございません。Azure OpenAI サービスへの接続に失敗しました。\n\nエラー: {ex.Message}";
                await turnContext.SendActivityAsync(MessageFactory.Text(errorMessage, errorMessage), cancellationToken);
                _logger.LogError(ex, "Azure OpenAI request failed: {ErrorCode} - {Message}", ex.ErrorCode, ex.Message);
            }
            catch (Exception ex)
            {
                var errorMessage = $"申し訳ございません。予期しないエラーが発生しました。\n\nエラー: {ex.Message}";
                await turnContext.SendActivityAsync(MessageFactory.Text(errorMessage, errorMessage), cancellationToken);
                _logger.LogError(ex, "Unexpected error in OnMessageActivityAsync: {Message}", ex.Message);
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

        private async Task<ChatCompletions> generateMessage(List<GptMessage> messages, float temperature = 0.0f, int maxTokens = 500)
        {
            var requestMessages = new List<ChatRequestMessage>();
            foreach (var message in messages)
            {
                switch (message.Role)
                {
                    case "user":
                        requestMessages.Add(new ChatRequestUserMessage(message.Content));
                        break;
                    case "assistant":
                        requestMessages.Add(new ChatRequestAssistantMessage(message.Content));
                        break;
                    case "system":
                        requestMessages.Add(new ChatRequestSystemMessage(message.Content));
                        break;
                }
            }

            var chatCompletionsOptions = new ChatCompletionsOptions(_configuration["OpenAIDeployment"], requestMessages);
            chatCompletionsOptions.Temperature = temperature;
            chatCompletionsOptions.MaxTokens = maxTokens;
            Response<ChatCompletions> response = await _openAIClient.GetChatCompletionsAsync(chatCompletionsOptions);
            return response.Value;
        }
    }
}
