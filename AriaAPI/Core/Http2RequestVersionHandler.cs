// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;


namespace AriaAPI.Core
{

    /// <summary>
    /// Negotiates HTTP/2 support on the first HTTPS request and caches the result
    /// for all subsequent requests.
    /// <para>
    /// <b>Threading:</b> The static <c>_h2Supported</c> field is shared across all
    /// instances. The first request that completes the probe sets the value via
    /// <see cref="Interlocked.CompareExchange(ref int, int, int)"/>, which is atomic.
    /// A narrow race window exists where multiple threads may simultaneously see
    /// <c>-1</c> (unknown) and each send a probe request with HTTP/2 + fallback.
    /// This is harmless because <c>RequestVersionOrLower</c> guarantees a valid
    /// response regardless, and only the first <c>CompareExchange</c> to complete
    /// will set the cached value — subsequent attempts are no-ops.
    /// </para>
    /// </summary>
    public sealed class Http2NegotiationHandler : DelegatingHandler
    {
        private static int _h2Supported = -1; // -1 unknown, 0 no, 1 yes
        private readonly Uri _baseUri;

        /// <summary>
        /// Initializes a new instance of the <see cref="Http2NegotiationHandler"/> class.
        /// </summary>
        /// <param name="inner">The inner HTTP message handler to delegate to.</param>
        /// <param name="baseUri">The FHIR server base URI used to determine whether HTTP/2 probing should occur (HTTPS only).</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="baseUri"/> is <see langword="null"/>.</exception>
        public Http2NegotiationHandler(HttpMessageHandler inner, Uri baseUri) : base(inner)
        {
            _baseUri = baseUri ?? throw new ArgumentNullException(nameof(baseUri));
        }

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
#if NET6_0_OR_GREATER
            if (Volatile.Read(ref _h2Supported) == -1 && _baseUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                // First request: try h2 with fallback
                request.Version = HttpVersion.Version20;
                request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
                var resp = await base.SendAsync(request, ct).ConfigureAwait(false);
                Interlocked.CompareExchange(ref _h2Supported, resp.Version.Major >= 2 ? 1 : 0, -1);
                return resp;
            }

            if (Volatile.Read(ref _h2Supported) == 1)
            {
                request.Version = HttpVersion.Version20;
                request.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
            }
            // if 0 (no h2), do nothing (default 1.1)
#endif
            return await base.SendAsync(request, ct).ConfigureAwait(false);
        }
    }

}
