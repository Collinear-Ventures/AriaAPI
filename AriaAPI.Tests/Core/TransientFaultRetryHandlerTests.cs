// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AriaAPI.Core;
using Microsoft.Extensions.Logging.Abstractions;
using RichardSzalay.MockHttp;
using Xunit;

namespace AriaAPI.Tests.Core
{
    public sealed class TransientFaultRetryHandlerTests
    {
        private static HttpClient BuildClient(MockHttpMessageHandler mock, int maxAttempts = 3)
        {
            var handler = new TransientFaultRetryHandler(mock, NullLogger.Instance,
                maxAttempts: maxAttempts, baseDelay: TimeSpan.FromMilliseconds(1));
            return new HttpClient(handler) { BaseAddress = new Uri("http://test.local/") };
        }

        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new TransientFaultRetryHandler(new MockHttpMessageHandler(), null!));
        }

        [Fact]
        public void Constructor_ZeroMaxAttempts_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new TransientFaultRetryHandler(new MockHttpMessageHandler(), NullLogger.Instance, maxAttempts: 0));
        }

        [Fact]
        public void Constructor_NegativeMaxAttempts_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new TransientFaultRetryHandler(new MockHttpMessageHandler(), NullLogger.Instance, maxAttempts: -1));
        }

        [Fact]
        public async Task SuccessResponse_ReturnedImmediately()
        {
            var mock = new MockHttpMessageHandler();
            mock.When("*").Respond(HttpStatusCode.OK, "application/json", "{}");
            var client = BuildClient(mock);

            var resp = await client.GetAsync("http://test.local/resource");

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        }

        [Fact]
        public async Task NotFound404_NotRetried_ReturnedImmediately()
        {
            var mock = new MockHttpMessageHandler();
            mock.When("*").Respond(HttpStatusCode.NotFound, "application/json", "{}");
            var client = BuildClient(mock);

            var resp = await client.GetAsync("http://test.local/resource");

            Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
        }

        [Fact]
        public async Task Service503_ThenSuccess_ReturnsSuccess()
        {
            var mock = new MockHttpMessageHandler();
            // First call returns 503, second returns 200
            mock.When("http://test.local/resource")
                .Respond(HttpStatusCode.ServiceUnavailable);
            // MockHttp falls through to next matching handler after first match
            // Use a sequence approach:
            var callCount = 0;
            var handler = new FuncHandler(r =>
            {
                callCount++;
                if (callCount == 1)
                    return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                return new HttpResponseMessage(HttpStatusCode.OK)
                    { Content = new StringContent("{}") };
            });
            var retryHandler = new TransientFaultRetryHandler(handler, NullLogger.Instance,
                maxAttempts: 3, baseDelay: TimeSpan.FromMilliseconds(1));
            var client = new HttpClient(retryHandler) { BaseAddress = new Uri("http://test.local/") };

            var resp = await client.GetAsync("http://test.local/resource");

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            Assert.Equal(2, callCount);
        }

        [Fact]
        public async Task CancellationDuringRetryDelay_ThrowsOperationCanceledException()
        {
            var handler = new FuncHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            var retryHandler = new TransientFaultRetryHandler(handler, NullLogger.Instance,
                maxAttempts: 5, baseDelay: TimeSpan.FromSeconds(10)); // long delay to ensure cancellation
            var client = new HttpClient(retryHandler) { BaseAddress = new Uri("http://test.local/") };

            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                client.GetAsync("http://test.local/resource", cts.Token));
        }

        // Helper for custom response logic
        private sealed class FuncHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _func;
            public FuncHandler(Func<HttpRequestMessage, HttpResponseMessage> func) => _func = func;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
                => Task.FromResult(_func(request));
        }
    }
}
