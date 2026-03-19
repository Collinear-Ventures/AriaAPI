// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AriaAPI.Core
{
    /// <summary>
    /// HTTP delegating handler that retries on transient FHIR server failures:
    /// <see cref="HttpStatusCode.ServiceUnavailable"/> (503),
    /// <see cref="HttpStatusCode.TooManyRequests"/> (429),
    /// and network-level <see cref="HttpRequestException"/>.
    /// Uses exponential backoff (base delay doubles each retry).
    /// </summary>
    /// <remarks>
    /// Default: 3 total attempts (1 initial + 2 retries), 1-second base delay.
    /// Non-retriable responses (4xx except 429, other 5xx) are returned immediately.
    /// Cancellation tokens are honoured between retries.
    /// </remarks>
    public sealed class TransientFaultRetryHandler : DelegatingHandler
    {
        private readonly int _maxAttempts;
        private readonly TimeSpan _baseDelay;
        private readonly ILogger _logger;

        private static readonly HttpStatusCode[] RetriableStatusCodes =
        {
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.TooManyRequests,
        };

        /// <summary>
        /// Initializes a new instance of <see cref="TransientFaultRetryHandler"/>.
        /// </summary>
        /// <param name="inner">Inner handler in the pipeline.</param>
        /// <param name="logger">Logger for retry diagnostics.</param>
        /// <param name="maxAttempts">Total attempts including the initial one. Must be ≥ 1.</param>
        /// <param name="baseDelay">Base wait between retries. Doubles each attempt. Defaults to 1 second.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="inner"/> or <paramref name="logger"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="maxAttempts"/> is less than 1.
        /// </exception>
        public TransientFaultRetryHandler(
            HttpMessageHandler inner,
            ILogger logger,
            int maxAttempts = 3,
            TimeSpan? baseDelay = null)
            : base(inner)
        {
            ArgumentNullException.ThrowIfNull(logger);
            if (maxAttempts < 1)
                throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Must be at least 1.");

            _logger = logger;
            _maxAttempts = maxAttempts;
            _baseDelay = baseDelay ?? TimeSpan.FromSeconds(1);
        }

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage? response = null;
            Exception? lastException = null;

            for (int attempt = 1; attempt <= _maxAttempts; attempt++)
            {
                try
                {
                    response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

                    if (!IsRetriable(response.StatusCode))
                        return response;

                    _logger.LogWarning(
                        "Transient FHIR server error {Status} on attempt {Attempt}/{Max} — {Path}",
                        (int)response.StatusCode, attempt, _maxAttempts, request.RequestUri?.AbsolutePath);

                    response.Dispose();
                    response = null;
                }
                catch (HttpRequestException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    lastException = ex;
                    _logger.LogWarning(ex,
                        "Network error on attempt {Attempt}/{Max} — {Path}",
                        attempt, _maxAttempts, request.RequestUri?.AbsolutePath);
                }

                if (attempt < _maxAttempts)
                {
                    var delay = TimeSpan.FromTicks(_baseDelay.Ticks * (long)Math.Pow(2, attempt - 1));
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }

            if (lastException is not null)
                throw lastException;

            // All retriable attempts exhausted — one final attempt, return whatever comes back
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        private static bool IsRetriable(HttpStatusCode code)
        {
            foreach (var c in RetriableStatusCodes)
                if (c == code) return true;
            return false;
        }
    }
}
