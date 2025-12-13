// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
