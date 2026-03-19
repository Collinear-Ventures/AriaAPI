// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿
using AriaAPI.Core;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace AriaAPI.Security
{
    /// <summary>
    /// Provides access tokens via client credentials and caches them by (system, scope).
    /// </summary>
    public class TokenProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly FhirClientFactory _factory;

        private static readonly TimeSpan ExpirySkew = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenProvider"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client used to request tokens from the OAuth2 token endpoint.</param>
        /// <param name="cache">The memory cache used to store tokens until they expire (minus skew).</param>
        /// <param name="factory">The FHIR client factory used to read the active system configuration.</param>
        public TokenProvider(HttpClient httpClient, IMemoryCache cache, FhirClientFactory factory)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>
        /// Acquire an access token. If <paramref name="scopeOverride"/> is null or empty,
        /// the scope from the active system configuration is used.
        /// </summary>
        public async Task<string> GetTokenAsync(string? scopeOverride = null, CancellationToken ct = default)
        {
            // Read active system (Test/Prod) from strongly-typed options
            var system = _factory.GetActiveSystem(out var systemName);

            var scope = string.IsNullOrWhiteSpace(scopeOverride)
                ? system.Auth.Scope
                : scopeOverride;

            if (scope is null)
                throw new InvalidOperationException($"Missing scope for FHIR system '{systemName}'.");

            var scopeNormalized = NormalizeScope(scope);

            // Cache key includes system to avoid cross-environment collisions
            var cacheKey = $"token:{systemName}:{scopeNormalized}";
            if (_cache.TryGetValue<string>(cacheKey, out var cached) && !string.IsNullOrWhiteSpace(cached))
                return cached!;

            var tokenUrl = NormalizeTokenEndpoint(system.Auth.Authority);
            var clientId = system.Auth.ClientId
                ?? throw new InvalidOperationException($"Missing ClientId for FHIR system '{systemName}'.");
            var clientSecret = system.Auth.ClientSecret
                ?? throw new InvalidOperationException($"Missing ClientSecret for FHIR system '{systemName}'.");

            var values = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["scope"] = scopeNormalized
            };

            using var content = new FormUrlEncodedContent(values);
            using var response = await _httpClient.PostAsync(tokenUrl, content, ct).ConfigureAwait(false);

            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Token request failed with status {(int)response.StatusCode}.");

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var tr = JsonSerializer.Deserialize<TokenResponse>(body, opts)
                     ?? throw new InvalidOperationException("Failed to parse token response.");
            if (string.IsNullOrWhiteSpace(tr.AccessToken))
                throw new InvalidOperationException("Token response contained empty access token.");

            var expiresIn = TimeSpan.FromSeconds(tr.ExpiresIn ?? 3600);
            var cacheDuration = expiresIn > ExpirySkew ? (expiresIn - ExpirySkew) : TimeSpan.FromSeconds(60);
            _cache.Set(cacheKey, tr.AccessToken!, cacheDuration);

            return tr.AccessToken!;
        }

        /// <summary>
        /// Normalizes scope inputs allowing comma or space lists -> single space-delimited string.
        /// </summary>
        private static string NormalizeScope(string scope) =>
            string.Join(" ", scope.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries));

        /// <summary>
        /// Accept either a full token endpoint (.../connect/token) or an authority base URL and append /connect/token.
        /// </summary>
        private static string NormalizeTokenEndpoint(string authorityOrTokenEndpoint)
        {
            if (string.IsNullOrWhiteSpace(authorityOrTokenEndpoint))
                throw new InvalidOperationException("Token endpoint/authority not configured.");

            // If it already looks like a token endpoint, return as-is
            var lower = authorityOrTokenEndpoint.ToLowerInvariant();
            if (lower.EndsWith("/connect/token") || lower.Contains("/tokenservice/connect/token"))
                return authorityOrTokenEndpoint;

            // Otherwise treat it as an authority base and append standard token path
            return authorityOrTokenEndpoint.TrimEnd('/') + "/connect/token";
        }

        private record TokenResponse(
            [property: JsonPropertyName("access_token")] string? AccessToken,
            [property: JsonPropertyName("expires_in")] int? ExpiresIn);
    }
}
