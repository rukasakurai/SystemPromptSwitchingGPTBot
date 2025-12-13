// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;

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
                logger.LogError(exception, $"[OnTurnError] unhandled error : {exception.Message}");

                // Check for ErrorResponseException to log detailed authentication information
                if (exception is HttpOperationException httpException)
                {
                    logger.LogError("HttpOperationException details:");
                    logger.LogError("  Request URL: {RequestUrl}", httpException.Request?.RequestUri?.ToString() ?? "N/A");
                    logger.LogError("  Request Method: {RequestMethod}", httpException.Request?.Method?.ToString() ?? "N/A");
                    logger.LogError("  Response Status Code: {StatusCode}", httpException.Response?.StatusCode.ToString() ?? "N/A");
                    logger.LogError("  Response Content: {ResponseContent}", httpException.Response?.Content ?? "N/A");
                    
                    // Log request headers (credentials info)
                    if (httpException.Request?.Headers != null)
                    {
                        logger.LogError("  Request Headers:");
                        foreach (var header in httpException.Request.Headers)
                        {
                            // Mask sensitive authorization header values
                            var headerValue = header.Key.Equals("Authorization", System.StringComparison.OrdinalIgnoreCase) 
                                ? "[REDACTED]" 
                                : string.Join(", ", header.Value);
                            logger.LogError("    {HeaderKey}: {HeaderValue}", header.Key, headerValue);
                        }
                    }
                }

                // Check for common authentication issues and provide helpful messages
                string userMessage = "The bot encountered an error or bug.";
                string detailMessage = "To continue to run this bot, please fix the bot source code.";
                
                if (exception.Message.Contains("Unauthorized", System.StringComparison.OrdinalIgnoreCase) || 
                    exception.Message.Contains("401", System.StringComparison.OrdinalIgnoreCase) ||
                    exception.Message.Contains("authentication", System.StringComparison.OrdinalIgnoreCase))
                {
                    userMessage = "認証エラーが発生しました。ボットの設定を確認してください。";
                    detailMessage = "MicrosoftAppId、MicrosoftAppPassword、または MicrosoftAppTenantId の設定を確認してください。詳細はトラブルシューティングドキュメントを参照してください。";
                    logger.LogError("Authentication error detected. Please verify MicrosoftAppId, MicrosoftAppPassword, and MicrosoftAppTenantId configuration.");
                }

                // Send a message to the user
                await turnContext.SendActivityAsync(userMessage);
                await turnContext.SendActivityAsync(detailMessage);

                // Send a trace activity, which will be displayed in the Bot Framework Emulator
                await turnContext.TraceActivityAsync("OnTurnError Trace", exception.Message, "https://www.botframework.com/schemas/error", "TurnError");
            };
        }
    }
}
