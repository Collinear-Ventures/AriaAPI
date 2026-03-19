// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.Core;
using AriaAPI.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RichardSzalay.MockHttp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AriaAPI.Tests.Security
{
    /// <summary>
    /// Tests for <see cref="TokenProvider"/>: token acquisition, caching, scope normalisation,
    /// URL construction, and config validation.
    /// </summary>
    /// <remarks>
    /// All patient-like identifiers in this file use opaque fake values — no real MRNs or credentials
    /// appear anywhere in test data (HIPAA compliance).
    /// </remarks>
    public sealed class TokenProviderTests
    {
        private const string FakeToken = "fake-access-token-aabbcc";
        private const string TokenJson = $"{{\"access_token\":\"{FakeToken}\",\"expires_in\":3600}}";
        private const string TokenEndpoint = "https://auth.test.local/connect/token";
        private const string AuthorityBase = "https://auth.test.local";

        // ── factory helpers ────────────────────────────────────────────────────

        private static FhirOptions ValidOptions(
            string authority = AuthorityBase,
            string clientId = "test-client-id",
            string clientSecret = "test-client-secret",
            string scope = "fhir.read") =>
            new FhirOptions
            {
                ActiveSystem = "Test",
                Systems = new Dictionary<string, FhirSystemOptions>
                {
                    ["Test"] = new FhirSystemOptions
                    {
                        BaseUrl = "https://fhir.test.local/fhir",
                        Auth = new AuthOptions
                        {
                            Authority = authority,
                            ClientId = clientId,
                            ClientSecret = clientSecret,
                            Scope = scope
                        }
                    }
                }
            };

        private static FhirClientFactory BuildFactory(FhirOptions options) =>
            new FhirClientFactory(new TestOptionsMonitor<FhirOptions>(options));

        private static (TokenProvider provider, MockHttpMessageHandler mock) BuildProvider(
            string responseBody = TokenJson,
            FhirOptions? options = null)
        {
            var mock = new MockHttpMessageHandler();
            mock.When("*").Respond(HttpStatusCode.OK, "application/json", responseBody);
            var httpClient = new HttpClient(mock);
            var factory = BuildFactory(options ?? ValidOptions());
            var provider = new TokenProvider(httpClient, new MemoryCache(new MemoryCacheOptions()), factory);
            return (provider, mock);
        }

        // ── tests ─────────────────────────────────────────────────────────────

        /// <summary>Happy path: first call returns the token from the server.</summary>
        [Fact]
        public async Task GetTokenAsync_FirstCall_ReturnsAccessToken()
        {
            var (provider, _) = BuildProvider();

            var token = await provider.GetTokenAsync();

            Assert.Equal(FakeToken, token);
        }

        /// <summary>Second call within the cache window returns the cached token without a new HTTP request.</summary>
        [Fact]
        public async Task GetTokenAsync_SecondCallWithinCacheWindow_NoDuplicateHttpRequest()
        {
            var callCount = 0;
            var handler = new AsyncFuncHandler(async (req, ct) =>
            {
                Interlocked.Increment(ref callCount);
                await Task.CompletedTask;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(TokenJson, Encoding.UTF8, "application/json")
                };
            });
            var httpClient = new HttpClient(handler);
            var provider = new TokenProvider(httpClient, new MemoryCache(new MemoryCacheOptions()), BuildFactory(ValidOptions()));

            await provider.GetTokenAsync();
            await provider.GetTokenAsync();

            Assert.Equal(1, callCount);
        }

        /// <summary>A different scope produces a separate cache entry and a new HTTP request.</summary>
        [Fact]
        public async Task GetTokenAsync_DifferentScopeOverride_MakesNewHttpRequest()
        {
            var callCount = 0;
            var handler = new AsyncFuncHandler(async (req, ct) =>
            {
                Interlocked.Increment(ref callCount);
                await Task.CompletedTask;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(TokenJson, Encoding.UTF8, "application/json")
                };
            });
            var httpClient = new HttpClient(handler);
            var provider = new TokenProvider(httpClient, new MemoryCache(new MemoryCacheOptions()), BuildFactory(ValidOptions()));

            await provider.GetTokenAsync("scope-a");
            await provider.GetTokenAsync("scope-b");

            Assert.Equal(2, callCount);
        }

        /// <summary>Comma-delimited scope is converted to space-delimited in the POST body.</summary>
        [Fact]
        public async Task GetTokenAsync_CommaDelimitedScope_SentAsSpaceDelimited()
        {
            string? capturedScope = null;
            var handler = new AsyncFuncHandler(async (req, ct) =>
            {
                var body = await req.Content!.ReadAsStringAsync(ct);
                // form-encoded: scope=scope1+scope2+scope3 or scope=scope1%20scope2%20scope3
                var decoded = Uri.UnescapeDataString(body.Replace('+', ' '));
                foreach (var part in decoded.Split('&'))
                {
                    var kv = part.Split('=');
                    if (kv.Length == 2 && kv[0] == "scope")
                        capturedScope = kv[1];
                }
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(TokenJson, Encoding.UTF8, "application/json")
                };
            });
            var httpClient = new HttpClient(handler);
            var options = ValidOptions(scope: "scope1,scope2,scope3");
            var provider = new TokenProvider(httpClient, new MemoryCache(new MemoryCacheOptions()), BuildFactory(options));

            await provider.GetTokenAsync();

            Assert.Equal("scope1 scope2 scope3", capturedScope);
        }

        /// <summary>Authority ending with /connect/token is used verbatim, without appending a second path.</summary>
        [Fact]
        public async Task GetTokenAsync_AuthorityIsFullTokenEndpoint_UsedAsIs()
        {
            string? requestUrl = null;
            var handler = new AsyncFuncHandler(async (req, ct) =>
            {
                requestUrl = req.RequestUri?.ToString();
                await Task.CompletedTask;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(TokenJson, Encoding.UTF8, "application/json")
                };
            });
            var httpClient = new HttpClient(handler);
            var options = ValidOptions(authority: "https://auth.test.local/connect/token");
            var provider = new TokenProvider(httpClient, new MemoryCache(new MemoryCacheOptions()), BuildFactory(options));

            await provider.GetTokenAsync();

            Assert.Equal("https://auth.test.local/connect/token", requestUrl);
        }

        /// <summary>An authority base URL has /connect/token appended automatically.</summary>
        [Fact]
        public async Task GetTokenAsync_AuthorityBaseUrl_AppendsConnectTokenPath()
        {
            string? requestUrl = null;
            var handler = new AsyncFuncHandler(async (req, ct) =>
            {
                requestUrl = req.RequestUri?.ToString();
                await Task.CompletedTask;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(TokenJson, Encoding.UTF8, "application/json")
                };
            });
            var httpClient = new HttpClient(handler);
            var options = ValidOptions(authority: "https://auth.test.local");
            var provider = new TokenProvider(httpClient, new MemoryCache(new MemoryCacheOptions()), BuildFactory(options));

            await provider.GetTokenAsync();

            Assert.Equal("https://auth.test.local/connect/token", requestUrl);
        }

        /// <summary>Empty Authority causes an InvalidOperationException before any HTTP call is made.</summary>
        [Fact]
        public async Task GetTokenAsync_EmptyAuthority_ThrowsInvalidOperationException()
        {
            var (provider, _) = BuildProvider(options: ValidOptions(authority: ""));

            await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetTokenAsync());
        }

        /// <summary>Empty ClientId causes an InvalidOperationException.</summary>
        [Fact]
        public async Task GetTokenAsync_EmptyClientId_ThrowsInvalidOperationException()
        {
            var (provider, _) = BuildProvider(options: ValidOptions(clientId: ""));

            await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetTokenAsync());
        }

        /// <summary>Empty ClientSecret causes an InvalidOperationException.</summary>
        [Fact]
        public async Task GetTokenAsync_EmptyClientSecret_ThrowsInvalidOperationException()
        {
            var (provider, _) = BuildProvider(options: ValidOptions(clientSecret: ""));

            await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetTokenAsync());
        }

        /// <summary>
        /// When expires_in is larger than the 30-second skew, the cache duration is
        /// (expires_in - 30s), meaning the token will be re-fetched before actual expiry.
        /// </summary>
        [Fact]
        public async Task GetTokenAsync_LargeExpiresIn_CacheDurationReducedBySkew()
        {
            // 3660s expires_in → cache for 3630s (3660 - 30)
            var callCount = 0;
            var handler = new AsyncFuncHandler(async (req, ct) =>
            {
                Interlocked.Increment(ref callCount);
                await Task.CompletedTask;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "{\"access_token\":\"tok\",\"expires_in\":3660}",
                        Encoding.UTF8, "application/json")
                };
            });
            var httpClient = new HttpClient(handler);
            var provider = new TokenProvider(httpClient, new MemoryCache(new MemoryCacheOptions()), BuildFactory(ValidOptions()));

            await provider.GetTokenAsync();
            await provider.GetTokenAsync(); // should hit cache

            Assert.Equal(1, callCount);
        }

        /// <summary>
        /// When expires_in is at or below the 30-second skew, the fallback 60-second
        /// cache duration is used (not zero / immediate expiry).
        /// </summary>
        [Fact]
        public async Task GetTokenAsync_ExpiresInAtOrBelowSkew_FallbackCacheDurationApplied()
        {
            var callCount = 0;
            var handler = new AsyncFuncHandler(async (req, ct) =>
            {
                Interlocked.Increment(ref callCount);
                await Task.CompletedTask;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "{\"access_token\":\"tok\",\"expires_in\":30}",
                        Encoding.UTF8, "application/json")
                };
            });
            var httpClient = new HttpClient(handler);
            var provider = new TokenProvider(httpClient, new MemoryCache(new MemoryCacheOptions()), BuildFactory(ValidOptions()));

            await provider.GetTokenAsync();
            await provider.GetTokenAsync(); // still within 60s fallback → cache hit

            // Fallback cache means it's still cached, not immediately expired
            Assert.Equal(1, callCount);
        }

        /// <summary>Server returning a non-success status causes an InvalidOperationException.</summary>
        [Fact]
        public async Task GetTokenAsync_ServerReturnsError_ThrowsInvalidOperationException()
        {
            var mock = new MockHttpMessageHandler();
            mock.When("*").Respond(HttpStatusCode.Unauthorized, "application/json", "{\"error\":\"invalid_client\"}");
            var provider = new TokenProvider(
                new HttpClient(mock),
                new MemoryCache(new MemoryCacheOptions()),
                BuildFactory(ValidOptions()));

            await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetTokenAsync());
        }

        /// <summary>Server returning an empty access_token causes an InvalidOperationException.</summary>
        [Fact]
        public async Task GetTokenAsync_EmptyAccessToken_ThrowsInvalidOperationException()
        {
            var mock = new MockHttpMessageHandler();
            mock.When("*").Respond(HttpStatusCode.OK, "application/json", "{\"access_token\":\"\",\"expires_in\":3600}");
            var provider = new TokenProvider(
                new HttpClient(mock),
                new MemoryCache(new MemoryCacheOptions()),
                BuildFactory(ValidOptions()));

            await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetTokenAsync());
        }

        /// <summary>
        /// When the server returns an error, the exception message must include the numeric
        /// status code but must NOT leak the response body (CWE-209 mitigation).
        /// </summary>
        [Fact]
        public async Task GetTokenAsync_ServerReturnsError_ExceptionDoesNotContainResponseBody()
        {
            var mock = new MockHttpMessageHandler();
            mock.When("*").Respond(HttpStatusCode.Unauthorized, "application/json", "{\"error\":\"invalid_client\"}");
            var provider = new TokenProvider(
                new HttpClient(mock),
                new MemoryCache(new MemoryCacheOptions()),
                BuildFactory(ValidOptions()));

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetTokenAsync());

            Assert.Contains("401", ex.Message);
            Assert.DoesNotContain("invalid_client", ex.Message);
            Assert.DoesNotContain("error", ex.Message);
        }

        /// <summary>Constructor throws <see cref="ArgumentNullException"/> when httpClient is null.</summary>
        [Fact]
        public void Constructor_NullHttpClient_ThrowsArgumentNullException()
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var factory = BuildFactory(ValidOptions());

            Assert.Throws<ArgumentNullException>(() => new TokenProvider(null!, cache, factory));
        }

        /// <summary>Constructor throws <see cref="ArgumentNullException"/> when cache is null.</summary>
        [Fact]
        public void Constructor_NullCache_ThrowsArgumentNullException()
        {
            var httpClient = new HttpClient();
            var factory = BuildFactory(ValidOptions());

            Assert.Throws<ArgumentNullException>(() => new TokenProvider(httpClient, null!, factory));
        }

        /// <summary>Constructor throws <see cref="ArgumentNullException"/> when factory is null.</summary>
        [Fact]
        public void Constructor_NullFactory_ThrowsArgumentNullException()
        {
            var httpClient = new HttpClient();
            var cache = new MemoryCache(new MemoryCacheOptions());

            Assert.Throws<ArgumentNullException>(() => new TokenProvider(httpClient, cache, null!));
        }

        // ── helpers ────────────────────────────────────────────────────────────

        private sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
        {
            private readonly T _value;
            public TestOptionsMonitor(T value) => _value = value;
            public T CurrentValue => _value;
            public T Get(string? name) => _value;
            public IDisposable? OnChange(Action<T, string?> listener) => null;
        }

        private sealed class AsyncFuncHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _func;
            public AsyncFuncHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> func) => _func = func;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => _func(request, ct);
        }
    }
}
