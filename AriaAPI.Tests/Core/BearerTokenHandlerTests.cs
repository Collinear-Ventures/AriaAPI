// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.Core;
using AriaAPI.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AriaAPI.Tests.Core
{
    /// <summary>
    /// Tests for <see cref="ClientConfigurator.BearerTokenHandler"/>: token injection, 401 retry,
    /// no-infinite-retry, content cloning on retry, JWT expiry detection, and thread safety.
    /// </summary>
    /// <remarks>
    /// <see cref="ClientConfigurator.BearerTokenHandler"/> is <c>internal</c>; accessible via
    /// <c>InternalsVisibleTo("AriaAPI.Tests")</c> declared in the library csproj.
    /// All HTTP communication is mocked; no live endpoints are used.
    /// </remarks>
    public sealed class BearerTokenHandlerTests
    {
        private const string BaseUrl = "http://fhir.test.local/fhir/";
        private const string ResourceUrl = "http://fhir.test.local/fhir/Patient";

        // ── factory helpers ────────────────────────────────────────────────────

        private static FhirOptions ValidFhirOptions(string authority = "https://auth.test.local") =>
            new FhirOptions
            {
                ActiveSystem = "Test",
                Systems = new Dictionary<string, FhirSystemOptions>
                {
                    ["Test"] = new FhirSystemOptions
                    {
                        BaseUrl = BaseUrl,
                        Auth = new AuthOptions
                        {
                            Authority = authority,
                            ClientId = "test-client-id",
                            ClientSecret = "test-client-secret",
                            Scope = "fhir.read"
                        }
                    }
                }
            };

        private static FhirClientFactory BuildFactory(FhirOptions? options = null) =>
            new FhirClientFactory(new TestOptionsMonitor<FhirOptions>(options ?? ValidFhirOptions()));

        /// <summary>
        /// Builds a <see cref="TokenProvider"/> backed by a custom message handler for the token endpoint.
        /// </summary>
        private static TokenProvider BuildTokenProvider(HttpMessageHandler tokenHandler, FhirOptions? options = null)
        {
            var httpClient = new HttpClient(tokenHandler);
            return new TokenProvider(httpClient, new MemoryCache(new MemoryCacheOptions()), BuildFactory(options));
        }

        /// <summary>
        /// Builds a <see cref="ClientConfigurator.BearerTokenHandler"/> with the supplied inner (FHIR) handler
        /// and a token provider backed by the supplied token handler.
        /// </summary>
        private static (ClientConfigurator.BearerTokenHandler bearer, HttpClient client)
            BuildBearerClient(
                HttpMessageHandler fhirInner,
                HttpMessageHandler tokenInner,
                string scope = "fhir.read")
        {
            var tokenProvider = BuildTokenProvider(tokenInner);
            var bearer = new ClientConfigurator.BearerTokenHandler(tokenProvider, scope);
            // Replace the SocketsHttpHandler set in the constructor with our mock.
            bearer.InnerHandler = fhirInner;
            var client = new HttpClient(bearer) { BaseAddress = new Uri(BaseUrl) };
            return (bearer, client);
        }

        // ── JWT helper ─────────────────────────────────────────────────────────

        /// <summary>Creates a minimal JWT whose payload contains only the <c>exp</c> claim.</summary>
        private static string BuildJwt(DateTimeOffset expiry)
        {
            var expSeconds = expiry.ToUnixTimeSeconds();
            var payloadJson = $"{{\"exp\":{expSeconds}}}";
            var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
            var payloadBase64 = Convert.ToBase64String(payloadBytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
            return $"eyJhbGciOiJub25lIn0.{payloadBase64}.sig";
        }

        // ── token response helpers ─────────────────────────────────────────────

        private static HttpResponseMessage OkTokenResponse(string token = "test-bearer-token", int expiresIn = 3600) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $"{{\"access_token\":\"{token}\",\"expires_in\":{expiresIn}}}",
                    Encoding.UTF8, "application/json")
            };

        private static HttpResponseMessage OkFhirResponse() =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/fhir+json")
            };

        // ── tests ─────────────────────────────────────────────────────────────

        /// <summary>Constructor throws when <c>tokenProvider</c> is null.</summary>
        [Fact]
        public void Constructor_NullTokenProvider_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new ClientConfigurator.BearerTokenHandler(null!, "scope"));
        }

        /// <summary>Constructor throws when <c>scope</c> is null.</summary>
        [Fact]
        public void Constructor_NullScope_ThrowsArgumentNullException()
        {
            var tokenProvider = BuildTokenProvider(new SyncFuncHandler(_ => OkTokenResponse()));
            Assert.Throws<ArgumentNullException>(() =>
                new ClientConfigurator.BearerTokenHandler(tokenProvider, null!));
        }

        /// <summary>Outgoing request carries <c>Authorization: Bearer &lt;token&gt;</c>.</summary>
        [Fact]
        public async Task SendAsync_OutgoingRequest_HasBearerAuthorizationHeader()
        {
            AuthenticationHeaderValue? capturedAuth = null;
            var fhirHandler = new SyncFuncHandler(req =>
            {
                capturedAuth = req.Headers.Authorization;
                return OkFhirResponse();
            });
            var tokenHandler = new SyncFuncHandler(_ => OkTokenResponse("bearer-xyz"));

            var (_, client) = BuildBearerClient(fhirHandler, tokenHandler);
            using (client)
                await client.GetAsync(ResourceUrl);

            Assert.NotNull(capturedAuth);
            Assert.Equal("Bearer", capturedAuth!.Scheme);
            Assert.Equal("bearer-xyz", capturedAuth.Parameter);
        }

        /// <summary>
        /// When the server returns 401, the handler force-refreshes the token and retries the request exactly once.
        /// Both attempts carry an Authorization header.
        /// </summary>
        [Fact]
        public async Task SendAsync_UnauthorizedResponse_RetriesOnce()
        {
            var attemptCount = 0;
            var fhirHandler = new SyncFuncHandler(_ =>
            {
                return Interlocked.Increment(ref attemptCount) == 1
                    ? new HttpResponseMessage(HttpStatusCode.Unauthorized)
                    : OkFhirResponse();
            });
            var tokenHandler = new SyncFuncHandler(_ => OkTokenResponse());

            var (_, client) = BuildBearerClient(fhirHandler, tokenHandler);
            using (client)
            {
                var response = await client.GetAsync(ResourceUrl);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            Assert.Equal(2, attemptCount);
        }

        /// <summary>
        /// A second 401 (from the retry) is returned directly — the handler does NOT make a third attempt.
        /// </summary>
        [Fact]
        public async Task SendAsync_PersistentUnauthorized_NoInfiniteRetry()
        {
            var attemptCount = 0;
            var fhirHandler = new SyncFuncHandler(_ =>
            {
                Interlocked.Increment(ref attemptCount);
                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            });
            var tokenHandler = new SyncFuncHandler(_ => OkTokenResponse());

            var (_, client) = BuildBearerClient(fhirHandler, tokenHandler);
            using (client)
            {
                var response = await client.GetAsync(ResourceUrl);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }

            Assert.Equal(2, attemptCount); // initial + one retry, no more
        }

        /// <summary>
        /// A POST request with body content can be retried on 401 without throwing
        /// <see cref="ObjectDisposedException"/> on the content stream.
        /// </summary>
        [Fact]
        public async Task SendAsync_PostWithBodyContent_ContentPreservedOnRetry()
        {
            var attemptCount = 0;
            string? retryBody = null;

            var fhirHandler = new AsyncFuncHandler(async (req, ct) =>
            {
                var attempt = Interlocked.Increment(ref attemptCount);
                if (attempt == 1)
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);

                retryBody = await req.Content!.ReadAsStringAsync(ct);
                return OkFhirResponse();
            });
            var tokenHandler = new SyncFuncHandler(_ => OkTokenResponse());

            var (_, client) = BuildBearerClient(fhirHandler, tokenHandler);
            using (client)
            {
                var payload = new StringContent("{\"resourceType\":\"Patient\"}", Encoding.UTF8, "application/fhir+json");
                var response = await client.PostAsync(ResourceUrl, payload);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            Assert.Equal("{\"resourceType\":\"Patient\"}", retryBody);
        }

        /// <summary>
        /// A JWT with an <c>exp</c> far in the future (beyond ClockSkew = 1 minute) prevents
        /// a pre-emptive token refresh on the second request.
        /// </summary>
        [Fact]
        public async Task SendAsync_TokenWithFarFutureExp_NoPreemptiveRefreshOnSecondRequest()
        {
            var farFutureJwt = BuildJwt(DateTimeOffset.UtcNow.AddHours(2));

            var tokenCallCount = 0;
            var tokenHandler = new SyncFuncHandler(_ =>
            {
                Interlocked.Increment(ref tokenCallCount);
                return OkTokenResponse(farFutureJwt);
            });
            var fhirHandler = new SyncFuncHandler(_ => OkFhirResponse());

            var (_, client) = BuildBearerClient(fhirHandler, tokenHandler);
            using (client)
            {
                await client.GetAsync(ResourceUrl);
                await client.GetAsync(ResourceUrl);
            }

            // Token provider called only once — JWT exp is far away, no refresh needed
            Assert.Equal(1, tokenCallCount);
        }

        /// <summary>
        /// A JWT whose <c>exp</c> is within ClockSkew (1 minute) of now triggers a pre-emptive
        /// refresh on the next request — observable once the <see cref="TokenProvider"/> cache
        /// also expires (set via <c>expires_in=31</c> → 1-second cache window).
        /// </summary>
        [Fact]
        public async Task SendAsync_TokenNearExpiry_PreemptiveRefreshOnNextRequest()
        {
            // expires_in=31 → cacheDuration = 31s - 30s skew = 1 second.
            // exp = now + 30s → within the 1-minute ClockSkew → BearerTokenHandler treats token as expiring.
            // After waiting 1.2 s, BOTH the TokenProvider cache and the BearerTokenHandler expiry
            // have passed, so the second request causes a second HTTP call to the token endpoint.
            var tokenCallCount = 0;
            var tokenHandler = new AsyncFuncHandler(async (req, ct) =>
            {
                var count = Interlocked.Increment(ref tokenCallCount);
                await Task.CompletedTask;
                // First call: near-expiry JWT; subsequent calls: far-future JWT
                var jwt = count == 1
                    ? BuildJwt(DateTimeOffset.UtcNow.AddSeconds(30))  // within ClockSkew (1 min)
                    : BuildJwt(DateTimeOffset.UtcNow.AddHours(1));     // safe far-future token
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        $"{{\"access_token\":\"{jwt}\",\"expires_in\":31}}",
                        Encoding.UTF8, "application/json")
                };
            });
            var fhirHandler = new SyncFuncHandler(_ => OkFhirResponse());

            var (_, client) = BuildBearerClient(fhirHandler, tokenHandler);
            using (client)
            {
                await client.GetAsync(ResourceUrl);       // acquires near-expiry token; cached for ~1 s
                await Task.Delay(1_300);                  // wait for TokenProvider's 1-second cache to expire
                await client.GetAsync(ResourceUrl);       // IsTokenExpiring()=true + cache miss → new HTTP call
            }

            // Two HTTP calls to the token endpoint: initial + pre-emptive refresh after cache/JWT expiry
            Assert.Equal(2, tokenCallCount);
        }

        /// <summary>
        /// Two concurrent requests on a fresh handler (no cached token) produce only one
        /// call to the <see cref="TokenProvider"/> — the SemaphoreSlim double-check prevents
        /// duplicate acquisition.
        /// </summary>
        [Fact]
        public async Task SendAsync_ConcurrentRequestsOnFreshHandler_SingleTokenAcquisition()
        {
            var tokenCallCount = 0;
            var gate = new SemaphoreSlim(0, 1); // starts closed — holds first token request

            var tokenHandler = new AsyncFuncHandler(async (req, ct) =>
            {
                Interlocked.Increment(ref tokenCallCount);
                await gate.WaitAsync(ct); // block until we explicitly release
                return OkTokenResponse(BuildJwt(DateTimeOffset.UtcNow.AddHours(1)));
            });
            var fhirHandler = new SyncFuncHandler(_ => OkFhirResponse());

            var tokenProvider = BuildTokenProvider(tokenHandler);
            // Use a single handler instance so both requests share the same _accessToken state
            var bearer = new ClientConfigurator.BearerTokenHandler(tokenProvider, "fhir.read");
            bearer.InnerHandler = fhirHandler;
            using var client = new HttpClient(bearer) { BaseAddress = new Uri(BaseUrl) };

            // Start two concurrent requests before any token is in flight
            var t1 = client.GetAsync(ResourceUrl);
            var t2 = client.GetAsync(ResourceUrl);

            // Short pause to let both tasks reach the token-acquisition lock
            await Task.Delay(50);

            // Release the gate once — first token request completes, second skips via double-check
            gate.Release(1);

            await Task.WhenAll(t1, t2);

            // Only ONE token request should have reached the HTTP handler
            Assert.Equal(1, tokenCallCount);
        }

        // ── inner-handler stubs ────────────────────────────────────────────────

        private sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
        {
            private readonly T _value;
            public TestOptionsMonitor(T value) => _value = value;
            public T CurrentValue => _value;
            public T Get(string? name) => _value;
            public IDisposable? OnChange(Action<T, string?> listener) => null;
        }

        private sealed class SyncFuncHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _func;
            public SyncFuncHandler(Func<HttpRequestMessage, HttpResponseMessage> func) => _func = func;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
                => Task.FromResult(_func(request));
        }

        private sealed class AsyncFuncHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _func;
            public AsyncFuncHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> func) => _func = func;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
                => _func(request, ct);
        }
    }
}
