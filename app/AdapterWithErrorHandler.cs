// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Logging;

namespace _07JP27.SystemPromptSwitchingGPTBot
{
    public class AdapterWithErrorHandler : CloudAdapter
    {
        public AdapterWithErrorHandler(BotFrameworkAuthentication auth, ILogger<IBotFrameworkHttpAdapter> logger)
            : base(auth, logger)
        {
            OnTurnError = async (turnContext, exception) =>
            {
                // Log any leaked exception from the application.
                // NOTE: In production environment, you should consider logging this to
                // Azure Application Insights. Visit https://aka.ms/bottelemetry to see how
                // to add telemetry capture to your bot.
                logger.LogError(exception, "[OnTurnError] Unhandled error in conversation. ActivityId: {activityId}, ConversationId: {conversationId}, Exception: {exceptionType}", 
                    turnContext.Activity?.Id,
                    turnContext.Activity?.Conversation?.Id,
                    exception.GetType().Name);

                // Send a message to the user
                var errorMessage = $"申し訳ございません。ボットでエラーが発生しました。\n\nエラーの種類: {exception.GetType().Name}\n詳細: {exception.Message}";
                await turnContext.SendActivityAsync(errorMessage);

                // Send a trace activity, which will be displayed in the Bot Framework Emulator
                await turnContext.TraceActivityAsync("OnTurnError Trace", exception.ToString(), "https://www.botframework.com/schemas/error", "TurnError");
            };
        }
    }
}
