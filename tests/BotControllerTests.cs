using System;
using System.Threading;
using System.Threading.Tasks;
using _07JP27.SystemPromptSwitchingGPTBot.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace SystemPromptSwitchingGPTBot.Tests
{
    /// <summary>
    /// Tests for BotController exception handling, particularly for authentication errors
    /// that can occur due to Conditional Access policies.
    /// </summary>
    public class BotControllerTests
    {
        private readonly Mock<IBotFrameworkHttpAdapter> _mockAdapter;
        private readonly Mock<IBot> _mockBot;
        private readonly Mock<ILogger<BotController>> _mockLogger;
        private readonly BotController _controller;
        private readonly DefaultHttpContext _httpContext;

        public BotControllerTests()
        {
            _mockAdapter = new Mock<IBotFrameworkHttpAdapter>();
            _mockBot = new Mock<IBot>();
            _mockLogger = new Mock<ILogger<BotController>>();
            _controller = new BotController(_mockAdapter.Object, _mockBot.Object, _mockLogger.Object);
            
            // Setup HttpContext
            _httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = _httpContext
            };
        }

        /// <summary>
        /// Helper method to verify that a log message was written at the specified log level.
        /// </summary>
        /// <param name="logLevel">The expected log level (e.g., Warning, Error)</param>
        /// <param name="expectedMessage">A substring that should be contained in the log message (not an exact match)</param>
        /// <param name="times">The expected number of times the log should have been written</param>
        private void VerifyLogMessage(LogLevel logLevel, string expectedMessage, Times times)
        {
            _mockLogger.Verify(
                x => x.Log(
                    logLevel,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                times);
        }

        [Fact]
        public async Task PostAsync_WithUnauthorizedAccessException_Returns200()
        {
            // Arrange
            _mockAdapter
                .Setup(a => a.ProcessAsync(It.IsAny<HttpRequest>(), It.IsAny<HttpResponse>(), It.IsAny<IBot>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedAccessException("Test unauthorized exception"));

            // Act
            await _controller.PostAsync();

            // Assert
            Assert.Equal(200, _httpContext.Response.StatusCode);
            VerifyLogMessage(LogLevel.Warning, "Authentication failure", Times.Once());
        }

        [Fact]
        public async Task PostAsync_WithAggregateException_ContainingAADSTS53003Error_Returns200()
        {
            // Arrange
            var innerException = new Exception("AADSTS53003: Access has been blocked by Conditional Access policies. Trace ID: 41ca209f-3430-4699-a4c9-78fd6827b300");
            var aggregateException = new AggregateException("Token acquisition failed", innerException);
            
            _mockAdapter
                .Setup(a => a.ProcessAsync(It.IsAny<HttpRequest>(), It.IsAny<HttpResponse>(), It.IsAny<IBot>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(aggregateException);

            // Act
            await _controller.PostAsync();

            // Assert
            Assert.Equal(200, _httpContext.Response.StatusCode);
            VerifyLogMessage(LogLevel.Warning, "Authentication error during token acquisition", Times.Once());
        }

        [Fact]
        public async Task PostAsync_WithAggregateException_ContainingConditionalAccessError_Returns200()
        {
            // Arrange
            var innerException = new Exception("Access blocked due to Conditional Access policy requirements");
            var aggregateException = new AggregateException("Authentication failed", innerException);
            
            _mockAdapter
                .Setup(a => a.ProcessAsync(It.IsAny<HttpRequest>(), It.IsAny<HttpResponse>(), It.IsAny<IBot>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(aggregateException);

            // Act
            await _controller.PostAsync();

            // Assert
            Assert.Equal(200, _httpContext.Response.StatusCode);
            VerifyLogMessage(LogLevel.Warning, "Authentication error during token acquisition", Times.Once());
        }

        [Fact]
        public async Task PostAsync_WithAggregateException_ContainingAuthenticationKeyword_Returns500()
        {
            // Arrange
            // Note: Generic "authentication" keyword is no longer treated as authentication error to avoid false positives
            var innerException = new Exception("Authentication credentials are not valid");
            var aggregateException = new AggregateException("Failed to process", innerException);
            
            _mockAdapter
                .Setup(a => a.ProcessAsync(It.IsAny<HttpRequest>(), It.IsAny<HttpResponse>(), It.IsAny<IBot>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(aggregateException);

            // Act
            await _controller.PostAsync();

            // Assert
            Assert.Equal(500, _httpContext.Response.StatusCode);
            VerifyLogMessage(LogLevel.Error, "Non-authentication AggregateException", Times.Once());
        }

        [Fact]
        public async Task PostAsync_WithNonAuthenticationAggregateException_Returns500()
        {
            // Arrange
            var innerException = new Exception("Some random network error occurred");
            var aggregateException = new AggregateException("Random failure", innerException);
            
            _mockAdapter
                .Setup(a => a.ProcessAsync(It.IsAny<HttpRequest>(), It.IsAny<HttpResponse>(), It.IsAny<IBot>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(aggregateException);

            // Act
            await _controller.PostAsync();

            // Assert
            Assert.Equal(500, _httpContext.Response.StatusCode);
            VerifyLogMessage(LogLevel.Error, "Non-authentication AggregateException", Times.Once());
        }

        [Fact]
        public async Task PostAsync_WithGenericException_Returns500()
        {
            // Arrange
            _mockAdapter
                .Setup(a => a.ProcessAsync(It.IsAny<HttpRequest>(), It.IsAny<HttpResponse>(), It.IsAny<IBot>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Some other error"));

            // Act
            await _controller.PostAsync();

            // Assert
            Assert.Equal(500, _httpContext.Response.StatusCode);
            VerifyLogMessage(LogLevel.Error, "Unhandled exception", Times.Once());
        }

        [Fact]
        public async Task PostAsync_SuccessfulProcessing_Returns200()
        {
            // Arrange
            _mockAdapter
                .Setup(a => a.ProcessAsync(It.IsAny<HttpRequest>(), It.IsAny<HttpResponse>(), It.IsAny<IBot>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _controller.PostAsync();

            // Assert - default status code is 200, and no errors are logged
            Assert.Equal(200, _httpContext.Response.StatusCode);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never());
        }

        [Fact]
        public async Task PostAsync_WithMultipleInnerExceptions_OneAuthenticationError_Returns200()
        {
            // Arrange
            var innerException1 = new Exception("Some random error");
            var innerException2 = new Exception("AADSTS50076: Multi-factor authentication is required");
            var aggregateException = new AggregateException("Multiple errors", innerException1, innerException2);
            
            _mockAdapter
                .Setup(a => a.ProcessAsync(It.IsAny<HttpRequest>(), It.IsAny<HttpResponse>(), It.IsAny<IBot>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(aggregateException);

            // Act
            await _controller.PostAsync();

            // Assert
            Assert.Equal(200, _httpContext.Response.StatusCode);
            VerifyLogMessage(LogLevel.Warning, "Authentication error during token acquisition", Times.Once());
        }

        [Fact]
        public async Task PostAsync_WithMultipleInnerExceptions_AllAuthenticationErrors_Returns200()
        {
            // Arrange
            var innerException1 = new Exception("AADSTS50076: Multi-factor authentication is required");
            var innerException2 = new Exception("AADSTS53003: Access has been blocked by Conditional Access policies");
            var aggregateException = new AggregateException("Multiple authentication errors", innerException1, innerException2);
            
            _mockAdapter
                .Setup(a => a.ProcessAsync(It.IsAny<HttpRequest>(), It.IsAny<HttpResponse>(), It.IsAny<IBot>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(aggregateException);

            // Act
            await _controller.PostAsync();

            // Assert
            Assert.Equal(200, _httpContext.Response.StatusCode);
            VerifyLogMessage(LogLevel.Warning, "Authentication error during token acquisition", Times.Once());
        }
    }
}
