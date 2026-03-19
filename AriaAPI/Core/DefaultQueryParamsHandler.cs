// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AriaAPI.Core
{
    /// <summary>
    /// HTTP delegating handler that injects default query parameters (such as <c>_total=none</c>)
    /// into outgoing FHIR search requests to reduce server-side computation. Continuation/paging
    /// links are left unchanged.
    /// </summary>
    public sealed class DefaultQueryParamsHandler : DelegatingHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultQueryParamsHandler"/> class.
        /// </summary>
        /// <param name="inner">The inner HTTP message handler to delegate to.</param>
        public DefaultQueryParamsHandler(HttpMessageHandler inner) : base(inner) { }

        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (request.Method == HttpMethod.Get && request.RequestUri is Uri uri)
            {
                // Skip continuation/next links (HAPI typically uses _getpages)
                var q = System.Web.HttpUtility.ParseQueryString(uri.Query);
                var isContinuation = q.AllKeys?.Any(k =>
                    string.Equals(k, "_getpages", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(k, "page", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(k, "cursor", StringComparison.OrdinalIgnoreCase)) == true;

                // Heuristic: treat "GET /{type}?..." as a search if path ends with a resource type and it has a query
                var hasQuery = !string.IsNullOrEmpty(uri.Query);
                if (!isContinuation && hasQuery && LooksLikeResourceSearch(uri))
                {
                    if (q.AllKeys?.Contains("_total") != true)
                        q["_total"] = "none";       // do not compute totals

                    //if (q.AllKeys?.Contains("_sort") != true)
                    //    q["_sort"] = "StartTime";        // index-friendly order for Appointment

                    var ub = new UriBuilder(uri) { Query = q.ToString()! };
                    request.RequestUri = ub.Uri;
                }
            }

            return base.SendAsync(request, ct);
        }

        private static bool LooksLikeResourceSearch(Uri uri)
        {
            // crude but effective: "/Appointment" or "/r4/Appointment" etc.
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0) return false;
            var last = segments[^1];
            // whitelist a few common resource types you search
            return string.Equals(last, "Appointment", StringComparison.OrdinalIgnoreCase)
                || string.Equals(last, "Patient", StringComparison.OrdinalIgnoreCase)
                || string.Equals(last, "Practitioner", StringComparison.OrdinalIgnoreCase);
        }
    }

}
