// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;


namespace AriaAPI.Core
{

    /// <summary>
    /// HTTP delegating handler that logs FHIR request and response details including
    /// method, URL, non-sensitive headers, status code, elapsed time, and content type.
    /// Sensitive headers (Authorization, API keys, cookies) are excluded from log output.
    /// </summary>
    public sealed class LoggingTimingHandler : DelegatingHandler
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Headers that must never be written to logs because they carry
        /// credentials, tokens, or session material.
        /// </summary>
        private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization",
            "X-Api-Key",
            "Proxy-Authorization",
            "Cookie",
            "Set-Cookie",
            "X-Client-Secret",
            "Password",
            "PhoneNumber",
            "SSN"
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingTimingHandler"/> class.
        /// </summary>
        /// <param name="inner">The inner HTTP message handler to delegate to.</param>
        /// <param name="logger">The logger used to record request and response details.</param>
        public LoggingTimingHandler(HttpMessageHandler inner, ILogger logger) : base(inner)
            => _logger = logger;

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // The exact URL + query you are sending
            _logger.LogDebug("FHIR REQUEST {Method} {Url}", request.Method, request.RequestUri);

            // Log only non-sensitive headers
            foreach (var h in request.Headers)
            {
                if (SensitiveHeaders.Contains(h.Key)) continue;
                _logger.LogDebug("  > {Header}: {Value}", h.Key, string.Join(",", h.Value));
            }

            // Optional: for POST/PUT, log body (be careful with PHI)
            // if (request.Content != null)
            //     _logger.LogDebug("  > Body: {Body}", await request.Content.ReadAsStringAsync(ct));

            var response = await base.SendAsync(request, ct).ConfigureAwait(false);

            sw.Stop();
            _logger.LogDebug("FHIR RESPONSE {Status} {ElapsedMs}ms {ContentType} {Url}",
                (int)response.StatusCode,
                sw.ElapsedMilliseconds,
                response.Content?.Headers?.ContentType?.ToString(),
                request.RequestUri);

            return response;
        }
    }


}
