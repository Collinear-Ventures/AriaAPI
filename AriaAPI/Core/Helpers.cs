// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using AriaAPI.Networking.Core;
using System.Threading.Tasks;

namespace AriaAPI.Core
{
    /// <summary>
    /// Helper methods for working with a <see cref="Networking.Core.AriaFhirClient{TResource}"/> and streaming or collecting FHIR resources.
    /// </summary>
    /// <remarks>
    /// This static class provides convenience methods to retrieve all resources from a FHIR server
    /// either as a single list (simple troubleshooting usage) or as an asynchronous stream with
    /// built-in retry/backoff logic and logging. Methods are designed to be safe to call from
    /// multiple threads, but the caller is responsible for coordinating access to the returned
    /// collections or for consuming the async stream.
    /// </remarks>
    public static class AriaFhirClientHelpers
    {

        /// <summary>
        /// Materializes all results from SearchAllAsync into a List{T}.
        /// </summary>
        public static async Task<List<T>> AggregateResourcesAsync<T>(this 
            AriaFhirClient<T> client,
            SearchParams searchParams,
            int? pageSize = null)
            where T : Resource
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(searchParams);

            var results = new List<T>();
            await foreach (var item in client.SearchAllAsync(searchParams, pageSize).ConfigureAwait(false))
            {
                results.Add(item);
            }
            return results;
        }


        /// <summary>
        /// Escapes a value for inclusion in a CSV field.
        /// Values containing commas, double-quotes, or newlines are wrapped in double-quotes,
        /// and any embedded double-quotes are doubled per RFC 4180.
        /// Returns an empty string when <paramref name="s"/> is <see langword="null"/> or empty.
        /// </summary>
        /// <param name="s">The raw string value to escape.</param>
        /// <returns>The CSV-safe string representation.</returns>
        public static string EscapeCsv(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            bool mustQuote = s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r');
            string t = s.Replace("\"", "\"\"");
            return mustQuote ? $"\"{t}\"" : t;
        }

        /// <summary>
        /// Lightweight value-object pairing a coding system, code, and display string.
        /// </summary>
        /// <param name="System">The coding system URI.</param>
        /// <param name="Code">The code value within the system.</param>
        /// <param name="Display">The human-readable display text.</param>
        public record CodeRow(string System, string Code, string Display);
    }

}