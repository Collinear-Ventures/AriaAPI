// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.Core;
using RichardSzalay.MockHttp;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AriaAPI.Tests.Core
{
    /// <summary>
    /// Tests for <see cref="CurrencySanitizerHandler"/>.
    /// </summary>
    public sealed class CurrencySanitizerHandlerTests
    {
        private const string TestUrl = "http://test.local/foo";

        private static HttpClient BuildClient(string mediaType, string body)
        {
            var mock = new MockHttpMessageHandler();
            mock.When(TestUrl).Respond(mediaType, body);
            var handler = new CurrencySanitizerHandler(mock);
            return new HttpClient(handler);
        }

        [Fact]
        public async Task NonJsonResponse_WithDollarSign_PassesThroughUnchanged()
        {
            var body = "price is $100";
            using var client = BuildClient("text/plain", body);

            var response = await client.GetAsync(TestUrl);
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal(body, result);
        }

        [Fact]
        public async Task JsonResponse_WithoutDollarSign_PassesThroughUnchanged()
        {
            var body = "{\"currency\":\"EUR\",\"amount\":42}";
            using var client = BuildClient("application/fhir+json", body);

            var response = await client.GetAsync(TestUrl);
            var result = await response.Content.ReadAsStringAsync();

            // Parse both as JSON values so whitespace differences don't matter
            Assert.Contains("\"EUR\"", result);
            Assert.DoesNotContain("\"USD\"", result);
        }

        [Fact]
        public async Task JsonResponse_WithCurrencyDollar_ReplacesDollarWithUsd()
        {
            var body = "{\"currency\":\"$\",\"amount\":42}";
            using var client = BuildClient("application/fhir+json", body);

            var response = await client.GetAsync(TestUrl);
            var result = await response.Content.ReadAsStringAsync();

            Assert.Contains("\"USD\"", result);
            Assert.DoesNotContain("\"$\"", result);
        }

        [Fact]
        public async Task JsonResponse_MalformedJson_PassesThroughUnchanged()
        {
            var body = "{\"currency\":\"$\", bad json{{";
            using var client = BuildClient("application/fhir+json", body);

            var response = await client.GetAsync(TestUrl);
            var result = await response.Content.ReadAsStringAsync();

            // Malformed JSON is returned as-is
            Assert.Equal(body, result);
        }

        [Fact]
        public async Task JsonResponse_WithCurrencyEur_NotModified()
        {
            var body = "{\"currency\":\"EUR\",\"amount\":10}";
            using var client = BuildClient("application/fhir+json", body);

            var response = await client.GetAsync(TestUrl);
            var result = await response.Content.ReadAsStringAsync();

            Assert.Contains("\"EUR\"", result);
            Assert.DoesNotContain("\"USD\"", result);
        }
        [Fact]
        public async Task JsonResponse_DeeplyNestedBeyondLimit_ThrowsInvalidOperationException()
        {
            var body = BuildDeeplyNestedJson(65);
            using var client = BuildClient("application/fhir+json", body);

            await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync(TestUrl));
        }

        [Fact]
        public async Task JsonResponse_NestedAtExactlyMaxDepth_ProcessesSuccessfully()
        {
            // 63 wrapping levels + 1 inner object = 64 total nesting depth,
            // with leaf value recursion reaching exactly depth 64 (the limit).
            var body = BuildDeeplyNestedJson(63);
            using var client = BuildClient("application/fhir+json", body);

            var response = await client.GetAsync(TestUrl);
            var result = await response.Content.ReadAsStringAsync();

            Assert.Contains("\"USD\"", result);
            Assert.DoesNotContain("\"$\"", result);
        }

        private static string BuildDeeplyNestedJson(int depth)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < depth; i++) sb.Append("{\"a\":");
            sb.Append("{\"currency\":\"$\"}");
            for (int i = 0; i < depth; i++) sb.Append('}');
            return sb.ToString();
        }
    }
}
