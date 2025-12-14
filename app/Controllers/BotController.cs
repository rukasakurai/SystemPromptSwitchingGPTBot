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
            // Note: The catch block for AggregateException is intentionally placed after UnauthorizedAccessException.
            // If an UnauthorizedAccessException is wrapped inside an AggregateException, it will be caught here
            // rather than by the more specific catch block above. This is the intended behavior.
            catch (AggregateException ex)
            {
                // Check if this is an authentication failure (e.g., MSAL token acquisition failure)
                bool isAuthenticationError = false;
                Exception firstAuthException = null;
                
                foreach (var innerEx in ex.InnerExceptions)
                {
                    if (IsAuthenticationException(innerEx))
                    {
                        isAuthenticationError = true;
                        if (firstAuthException == null)
                        {
                            firstAuthException = innerEx;
                        }
                    }
                }

                if (isAuthenticationError)
                {
                    // Log detailed authentication error information once
                    _logger.LogWarning(firstAuthException, 
                        "Authentication error during token acquisition. " +
                        "Error: {ErrorMessage}. " +
                        "This may be due to Conditional Access policies or other authentication restrictions.",
                        firstAuthException.Message);
                    
                    // Return 200 to prevent channel retries for authentication issues
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

        /// <summary>
        /// Determines if an exception is related to authentication failures.
        /// Checks for AADSTS error codes (Azure AD authentication errors) and Conditional Access policy blocks.
        /// </summary>
        /// <param name="ex">The exception to check</param>
        /// <returns>True if the exception is authentication-related, false otherwise</returns>
        private bool IsAuthenticationException(Exception ex)
        {
            var message = ex.Message;
            // Check for Azure AD authentication error codes (AADSTS) or Conditional Access policy blocks
            // Note: We intentionally avoid matching on the generic word "authentication" to reduce false positives
            return message.Contains("AADSTS", StringComparison.OrdinalIgnoreCase) ||
                   message.Contains("Conditional Access", StringComparison.OrdinalIgnoreCase);
        }
    }
}
