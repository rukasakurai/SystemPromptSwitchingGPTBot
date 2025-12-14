// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;

namespace _07JP27.SystemPromptSwitchingGPTBot.Controllers
{
    // This ASP Controller is created to handle a request. Dependency Injection will provide the Adapter and IBot
    // implementation at runtime. Multiple different IBot implementations running at different endpoints can be
    // achieved by specifying a more specific type for the bot constructor argument.
    [Route("api/messages")]
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly IBot _bot;
        private readonly ILogger<BotController> _logger;

        public BotController(IBotFrameworkHttpAdapter adapter, IBot bot, ILogger<BotController> logger)
        {
            _adapter = adapter;
            _bot = bot;
            _logger = logger;
        }

        [HttpPost, HttpGet]
        public async Task PostAsync()
        {
            // Delegate the processing of the HTTP POST to the adapter.
            // The adapter will invoke the bot.
            try
            {
                await _adapter.ProcessAsync(Request, Response, _bot);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Swallow authentication failures to avoid channel retries, but log them.
                _logger.LogWarning(ex, "Authentication failure while processing /api/messages. Returning 200 to avoid channel retries.");
                if (!Response.HasStarted)
                {
                    Response.StatusCode = 200;
                }
            }
            catch (AggregateException ex)
            {
                // Check if this is an authentication failure (e.g., MSAL token acquisition failure)
                bool isAuthenticationError = false;
                foreach (var innerEx in ex.InnerExceptions)
                {
                    var message = innerEx.Message;
                    // Check for common authentication error codes
                    if (message.Contains("AADSTS", StringComparison.OrdinalIgnoreCase) ||
                        message.Contains("authentication", StringComparison.OrdinalIgnoreCase) ||
                        message.Contains("Conditional Access", StringComparison.OrdinalIgnoreCase))
                    {
                        isAuthenticationError = true;
                        
                        // Log detailed authentication error information
                        _logger.LogWarning(innerEx, 
                            "Authentication error during token acquisition. " +
                            "Error: {ErrorMessage}. " +
                            "This may be due to Conditional Access policies or other authentication restrictions. " +
                            "Returning 200 to avoid channel retries.",
                            message);
                        
                        // Try to extract and log trace/correlation IDs if present
                        if (message.Contains("Trace ID:", StringComparison.OrdinalIgnoreCase) ||
                            message.Contains("Correlation ID:", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogWarning("Full error details: {FullMessage}", message);
                        }
                    }
                }

                if (isAuthenticationError)
                {
                    // Return 200 to prevent channel retries for authentication issues
                    _logger.LogWarning("Authentication-related AggregateException caught. Returning 200 to avoid channel retries.");
                    if (!Response.HasStarted)
                    {
                        Response.StatusCode = 200;
                    }
                }
                else
                {
                    // Non-authentication aggregate exception - log and return 500
                    _logger.LogError(ex, "Non-authentication AggregateException while processing /api/messages.");
                    if (!Response.HasStarted)
                    {
                        Response.StatusCode = 500;
                    }
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception while processing /api/messages.");
                if (!Response.HasStarted)
                {
                    Response.StatusCode = 500;
                }
            }
        }
    }
}
