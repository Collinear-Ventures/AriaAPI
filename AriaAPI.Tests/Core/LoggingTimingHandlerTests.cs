// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.Core;
using Microsoft.Extensions.Logging;
using RichardSzalay.MockHttp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AriaAPI.Tests.Core
{
    /// <summary>
    /// Tests for <see cref="LoggingTimingHandler"/>.
    /// </summary>
    public sealed class LoggingTimingHandlerTests
    {
        private const string TestUrl = "http://test.local/resource";

        private static (HttpClient client, CapturingLogger logger) BuildClient(
            string mediaType = "application/fhir+json",
            string body = "{}",
            HttpStatusCode status = HttpStatusCode.OK)
        {
            var mock = new MockHttpMessageHandler();
            mock.When(TestUrl).Respond(status, mediaType, body);

            var logger = new CapturingLogger();
            var handler = new LoggingTimingHandler(mock, logger);
            return (new HttpClient(handler), logger);
        }

        private static (HttpClient client, CapturingLogger logger) BuildClientWithLogLevel(
            LogLevel captureLevel,
            string mediaType = "application/fhir+json",
            string body = "{}",
            HttpStatusCode status = HttpStatusCode.OK)
        {
            var mock = new MockHttpMessageHandler();
            mock.When(TestUrl).Respond(status, mediaType, body);

            var logger = new CapturingLogger(captureLevel);
            var handler = new LoggingTimingHandler(mock, logger);
            return (new HttpClient(handler), logger);
        }

        [Fact]
        public async Task SendAsync_ResponseIsReturnedUnmodified()
        {
            var (client, _) = BuildClient(body: "{\"id\":\"1\"}", status: HttpStatusCode.OK);
            using (client)
            {
                var response = await client.GetAsync(TestUrl);
                var content = await response.Content.ReadAsStringAsync();

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal("{\"id\":\"1\"}", content);
            }
        }

        [Fact]
        public async Task SendAsync_GetMethod_AppearsInLogMessages()
        {
            var (client, logger) = BuildClient();
            using (client)
            {
                await client.GetAsync(TestUrl);
            }

            Assert.True(logger.Messages.Any(m => m.Contains("GET")),
                "Expected 'GET' to appear in log messages.");
        }

        [Fact]
        public async Task SendAsync_404Status_AppearsInLogMessages()
        {
            var (client, logger) = BuildClient(status: HttpStatusCode.NotFound);
            using (client)
            {
                await client.GetAsync(TestUrl);
            }

            Assert.True(logger.Messages.Any(m => m.Contains("404")),
                "Expected '404' to appear in log messages.");
        }

        [Fact]
        public async Task SendAsync_AuthorizationHeaderValue_NotLoggedAsSensitiveHeader()
        {
            var mock = new MockHttpMessageHandler();
            mock.When(TestUrl).Respond("application/fhir+json", "{}");

            var logger = new CapturingLogger();
            var handler = new LoggingTimingHandler(mock, logger);
            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer super-secret-token");

            await client.GetAsync(TestUrl);

            Assert.False(logger.Messages.Any(m => m.Contains("super-secret-token")),
                "Sensitive Authorization header value must not appear in any log message.");
        }

        [Fact]
        public async Task SendAsync_NonSensitiveCustomHeader_IsLogged()
        {
            var mock = new MockHttpMessageHandler();
            mock.When(TestUrl).Respond("application/fhir+json", "{}");

            var logger = new CapturingLogger();
            var handler = new LoggingTimingHandler(mock, logger);
            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Custom-Header", "my-custom-value");

            await client.GetAsync(TestUrl);

            Assert.True(logger.Messages.Any(m => m.Contains("my-custom-value")),
                "Expected non-sensitive custom header value to appear in log messages.");
        }

        [Fact]
        public async Task SendAsync_FhirRequestUrl_NotLoggedAtInformationLevel()
        {
            var (client, logger) = BuildClientWithLogLevel(LogLevel.Information);
            using (client)
            {
                await client.GetAsync(TestUrl);
            }

            Assert.False(logger.Messages.Any(m => m.Contains(TestUrl)),
                "FHIR request URL must not appear in Information-level log messages.");
        }

        [Fact]
        public async Task SendAsync_FhirRequestUrl_LoggedAtDebugLevel()
        {
            var (client, logger) = BuildClientWithLogLevel(LogLevel.Debug);
            using (client)
            {
                await client.GetAsync(TestUrl);
            }

            Assert.True(logger.Messages.Any(m => m.Contains(TestUrl)),
                "Expected FHIR request URL to appear in Debug-level log messages.");
        }

        [Fact]
        public async Task SendAsync_XClientSecretHeader_NotLoggedAsSensitiveHeader()
        {
            var mock = new MockHttpMessageHandler();
            mock.When(TestUrl).Respond("application/fhir+json", "{}");

            var logger = new CapturingLogger();
            var handler = new LoggingTimingHandler(mock, logger);
            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Client-Secret", "my-client-secret-value");

            await client.GetAsync(TestUrl);

            Assert.False(logger.Messages.Any(m => m.Contains("my-client-secret-value")),
                "Sensitive X-Client-Secret header value must not appear in any log message.");
        }

        [Fact]
        public async Task SendAsync_PasswordHeader_NotLoggedAsSensitiveHeader()
        {
            var mock = new MockHttpMessageHandler();
            mock.When(TestUrl).Respond("application/fhir+json", "{}");

            var logger = new CapturingLogger();
            var handler = new LoggingTimingHandler(mock, logger);
            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.TryAddWithoutValidation("Password", "my-password-value");

            await client.GetAsync(TestUrl);

            Assert.False(logger.Messages.Any(m => m.Contains("my-password-value")),
                "Sensitive Password header value must not appear in any log message.");
        }

        [Fact]
        public async Task SendAsync_PhoneNumberHeader_NotLoggedAsSensitiveHeader()
        {
            var mock = new MockHttpMessageHandler();
            mock.When(TestUrl).Respond("application/fhir+json", "{}");

            var logger = new CapturingLogger();
            var handler = new LoggingTimingHandler(mock, logger);
            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.TryAddWithoutValidation("PhoneNumber", "555-123-4567");

            await client.GetAsync(TestUrl);

            Assert.False(logger.Messages.Any(m => m.Contains("555-123-4567")),
                "Sensitive PhoneNumber header value must not appear in any log message.");
        }

        [Fact]
        public async Task SendAsync_SsnHeader_NotLoggedAsSensitiveHeader()
        {
            var mock = new MockHttpMessageHandler();
            mock.When(TestUrl).Respond("application/fhir+json", "{}");

            var logger = new CapturingLogger();
            var handler = new LoggingTimingHandler(mock, logger);
            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.TryAddWithoutValidation("SSN", "123-45-6789");

            await client.GetAsync(TestUrl);

            Assert.False(logger.Messages.Any(m => m.Contains("123-45-6789")),
                "Sensitive SSN header value must not appear in any log message.");
        }

        // ── CapturingLogger ────────────────────────────────────────────────────

        private sealed class CapturingLogger : ILogger
        {
            private readonly LogLevel? _filterLevel;

            public List<string> Messages { get; } = new List<string>();

            /// <summary>
            /// Creates a logger that captures all log levels.
            /// </summary>
            public CapturingLogger()
            {
                _filterLevel = null;
            }

            /// <summary>
            /// Creates a logger that only captures messages at the specified log level.
            /// IsEnabled returns true only for the specified level, so the handler
            /// will skip logging at other levels.
            /// </summary>
            /// <param name="filterLevel">The only log level that is enabled and captured.</param>
            public CapturingLogger(LogLevel filterLevel)
            {
                _filterLevel = filterLevel;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => _filterLevel is null || logLevel == _filterLevel;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (_filterLevel is not null && logLevel != _filterLevel) return;
                Messages.Add(formatter(state, exception));
            }
        }
    }
}
