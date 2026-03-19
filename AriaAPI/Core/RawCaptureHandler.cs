// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AriaAPI.Core
{
    /// <summary>
    /// Captures the raw request and response bodies for the current async context.
    /// <para>
    /// <b>Memory warning:</b> Captured bodies are stored in <see cref="AsyncLocal{T}"/>
    /// storage and persist for the lifetime of the async flow unless explicitly cleared
    /// via <see cref="Clear"/>. In long-running processes or tight loops, call
    /// <see cref="Clear"/> after consuming the captured data to avoid memory pressure.
    /// </para>
    /// </summary>
    internal sealed class RawCaptureHandler : DelegatingHandler
    {

        private static readonly AsyncLocal<Capture> _current = new();

        private static Capture CurrentCapture => _current.Value ??= new Capture();

        public static string? LastRequestBody => _current.Value?.LastRequestBody;
        public static string? LastResponseBody => _current.Value?.LastResponseBody;

        /// <summary>
        /// Clears captured request and response bodies for the current async context,
        /// releasing the associated memory.
        /// </summary>
        public static void Clear()
        {
            if (_current.Value is { } capture)
            {
                capture.LastRequestBody = null;
                capture.LastResponseBody = null;
            }
        }

        public event EventHandler<(HttpRequestMessage Request, string? RequestBody)>? OnRequestCaptured;
        public event EventHandler<(HttpResponseMessage Response, string? ResponseBody)>? OnResponseCaptured;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Capture request body (non-destructively).
            string? reqBody = null;
            if (request.Content is not null)
            {
                // Buffer original content into string.
                reqBody = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                // Rebuild content so downstream can still send it.
                var clone = new StringContent(reqBody);
                foreach (var h in request.Content.Headers)
                    clone.Headers.TryAddWithoutValidation(h.Key, h.Value);
                request.Content = clone;
            }

            CurrentCapture.LastRequestBody = reqBody;
            OnRequestCaptured?.Invoke(this, (request, reqBody));

            // Send
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            // Capture response body (non-destructively).
            string? respBody = null;
            if (response.Content is not null)
            {
                respBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                // Put back a readable content for downstream consumers.
                var clone = new StringContent(respBody);
                foreach (var h in response.Content.Headers)
                    clone.Headers.TryAddWithoutValidation(h.Key, h.Value);
                response.Content = clone;
            }

            CurrentCapture.LastResponseBody = respBody;
            OnResponseCaptured?.Invoke(this, (response, respBody));

            return response;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Clear();
            }
            base.Dispose(disposing);
        }

        private sealed class Capture
        {
            public string? LastRequestBody { get; set; }
            public string? LastResponseBody { get; set; }
        }

    }
}
