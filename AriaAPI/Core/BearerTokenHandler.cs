// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using AriaAPI.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AriaAPI.Core
{
    public sealed partial class ClientConfigurator : IDisposable
    {
        /// <summary>
        /// HTTP delegating handler that injects OAuth2 Bearer tokens, refreshes on expiry,
        /// and retries once on <see cref="HttpStatusCode.Unauthorized"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The handler:
        /// </para>
        /// <list type="bullet">
        ///   <item><description>Uses <see cref="TokenProvider"/> to acquire tokens scoped by the provided scope string.</description></item>
        ///   <item><description>Parses the JWT <c>exp</c> claim to determine token expiry (with a clock skew).</description></item>
        ///   <item><description>Buffers/clones outbound requests to support safe retries for requests with bodies.</description></item>
        ///   <item><description>Retries at most once per request when a 401 is encountered.</description></item>
        /// </list>
        /// </remarks>
        internal sealed class BearerTokenHandler : DelegatingHandler
        {
            private readonly TokenProvider _tokenProvider;
            private readonly string _scope;
            private string? _accessToken;
            private DateTimeOffset _expiresAtUtc = DateTimeOffset.MinValue;

            private readonly SemaphoreSlim _tokenLock = new(1, 1);
            private static readonly TimeSpan ClockSkew = TimeSpan.FromMinutes(1);

            /// <summary>
            /// Initializes a new instance of the <see cref="BearerTokenHandler"/> class.
            /// </summary>
            /// <param name="tokenProvider">Token acquisition service used to obtain and refresh bearer tokens.</param>
            /// <param name="scope">OAuth2 scope requested for tokens (e.g., <c>user/Patient.rs</c>).</param>
            /// <param name="maxConnectionsPerServer">
            /// Maximum number of concurrent HTTP connections allowed per server. Defaults to 50.
            /// </param>
            /// <exception cref="ArgumentNullException">
            /// Thrown when <paramref name="tokenProvider"/> or <paramref name="scope"/> is <c>null</c>.
            /// </exception>
            public BearerTokenHandler(TokenProvider tokenProvider, string scope, int maxConnectionsPerServer = 50)
            {
                _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
                _scope = scope ?? throw new ArgumentNullException(nameof(scope));

                // Terminate the handler chain with a SocketsHttpHandler tuned for reuse.
                InnerHandler = new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(15),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                    MaxConnectionsPerServer = maxConnectionsPerServer
                };
            }

            /// <summary>
            /// Sends an HTTP request with a valid Bearer token, refreshing and retrying once on 401.
            /// </summary>
            /// <param name="request">The request message.</param>
            /// <param name="cancellationToken">Cancellation token.</param>
            /// <returns>The HTTP response message.</returns>
            /// <remarks>
            /// The request is cloned and buffered before the first send to ensure the body can be resent on retry.
            /// </remarks>
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                // Always buffer a clone BEFORE first send so retries are safe for requests with bodies.
                var firstSend = await CloneForRetry(request).ConfigureAwait(false);

                var token = await GetValidAccessTokenAsync(cancellationToken).ConfigureAwait(false);
                firstSend.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await base.SendAsync(firstSend, cancellationToken).ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    response.Dispose();
                    await ForceRefreshAsync().ConfigureAwait(false);

                    token = await GetValidAccessTokenAsync(cancellationToken).ConfigureAwait(false);
                    var retry = await CloneForRetry(request).ConfigureAwait(false);
                    retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    return await base.SendAsync(retry, cancellationToken).ConfigureAwait(false);
                }

                return response;
            }

            /// <summary>
            /// Returns a valid access token, refreshing it if missing or near expiry.
            /// </summary>
            /// <param name="ct">Cancellation token.</param>
            /// <returns>A non-empty bearer token string.</returns>
            /// <exception cref="InvalidOperationException">Thrown when the token provider yields an empty token.</exception>
            private async Task<string> GetValidAccessTokenAsync(CancellationToken ct)
            {
                if (!IsTokenExpiring()) return _accessToken!;
                await _tokenLock.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    if (!IsTokenExpiring()) return _accessToken!;
                    var newToken = await _tokenProvider.GetTokenAsync(_scope, ct).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(newToken))
                        throw new InvalidOperationException("TokenProvider returned an empty access token.");

                    _accessToken = newToken;
                    _expiresAtUtc = GetExpiryFromJwt(newToken) ?? DateTimeOffset.UtcNow.AddMinutes(5);
                    return _accessToken!;
                }
                finally { _tokenLock.Release(); }
            }

            /// <summary>
            /// Forces the next request to acquire a fresh token by clearing the cached token and expiry.
            /// </summary>
            private async Task ForceRefreshAsync()
            {
                await _tokenLock.WaitAsync().ConfigureAwait(false);
                try { _expiresAtUtc = DateTimeOffset.MinValue; _accessToken = null; }
                finally { _tokenLock.Release(); }
            }

            /// <summary>
            /// Determines whether the current token is missing or within the configured clock skew of expiry.
            /// </summary>
            /// <returns><c>true</c> if a refresh is required; otherwise <c>false</c>.</returns>
            private bool IsTokenExpiring()
            {
                if (string.IsNullOrWhiteSpace(_accessToken)) return true;
                return DateTimeOffset.UtcNow.Add(ClockSkew) >= _expiresAtUtc;
            }

            /// <summary>
            /// Parses the JWT payload to read the <c>exp</c> (expiry) claim.
            /// </summary>
            /// <remarks>
            /// This method only decodes the JWT payload to read the <c>exp</c> claim without performing
            /// cryptographic signature verification. Full JWT validation (including signature, issuer,
            /// and audience checks) is deferred to a future enhancement.
            /// </remarks>
            /// <param name="jwt">A JWT access token.</param>
            /// <returns>
            /// A <see cref="DateTimeOffset"/> representing the expiry moment in UTC, or <c>null</c> if parsing fails.
            /// </returns>
            private static DateTimeOffset? GetExpiryFromJwt(string jwt)
            {
                try
                {
                    var parts = jwt.Split('.');
                    if (parts.Length < 2) return null;
                    var payload = parts[1].Replace('-', '+').Replace('_', '/');
                    payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '='); // pad
                    var bytes = Convert.FromBase64String(payload);
                    using var doc = JsonDocument.Parse(bytes);
                    if (!doc.RootElement.TryGetProperty("exp", out var expProp)) return null;
                    return DateTimeOffset.FromUnixTimeSeconds(expProp.GetInt64());
                }
                catch { return null; }
            }

            /// <summary>
            /// Produces a buffered clone of the request content suitable for retries.
            /// </summary>
            /// <param name="content">Original request content.</param>
            /// <returns>A new <see cref="HttpContent"/> instance or <c>null</c> if the original content was <c>null</c>.</returns>
            private static async Task<HttpContent?> CloneContentAsync(HttpContent? content)
            {
                if (content == null) return null;
                var ms = new MemoryStream();
                await content.CopyToAsync(ms).ConfigureAwait(false);
                ms.Position = 0;
                var clone = new StreamContent(ms);
                foreach (var h in content.Headers) clone.Headers.TryAddWithoutValidation(h.Key, h.Value);
                return clone;
            }

            /// <summary>
            /// Creates a buffered, header- and options-preserving clone of the specified request for safe retry.
            /// </summary>
            /// <param name="request">Request to clone.</param>
            /// <returns>A new <see cref="HttpRequestMessage"/> suitable for resending.</returns>
            private static async Task<HttpRequestMessage> CloneForRetry(HttpRequestMessage request)
            {
                var clone = new HttpRequestMessage(request.Method, request.RequestUri)
                {
#if NET5_0_OR_GREATER
                    Version = request.Version,
                    VersionPolicy = request.VersionPolicy
#endif
                };
                foreach (var header in request.Headers)
                    clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

                clone.Content = await CloneContentAsync(request.Content).ConfigureAwait(false);
#if NET6_0_OR_GREATER
                foreach (var opt in request.Options) clone.Options.TryAdd(opt.Key, opt.Value);
#endif
                return clone;
            }

            /// <summary>
            /// Disposes managed resources used by the handler.
            /// </summary>
            /// <param name="disposing">
            /// <c>true</c> if called from <see cref="IDisposable.Dispose"/>; otherwise <c>false</c>.
            /// </param>
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    base.Dispose(disposing);
                    _tokenLock.Dispose();
                }
            }
        }
    }
}