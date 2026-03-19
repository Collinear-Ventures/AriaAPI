// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.API.SearchHelpers;
using AriaAPI.Core;
using AriaAPI.Networking.Core;
using Hl7.Fhir.FhirPath;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static AriaAPI.API.SearchHelpers.SearchTypes;
using static AriaAPI.API.SingleResourceSearch.ValueSetSearch;

namespace AriaAPI.API.SingleResourceSearch
{
    /// <summary>
    /// Provides utilities to perform FHIR <c>ValueSet</c> <c>$expand</c> operations and
    /// return a flattened list of concepts (<c>system</c>, <c>code</c>, <c>display</c>).
    /// <para>
    /// This integrates with <see cref="ClientConfigurator"/> to obtain an authenticated <see cref="HttpClient"/>
    /// and is tolerant of servers that return <c>text/plain</c> with a FHIR JSON body.
    /// </para>
    /// </summary>
    public static class ValueSetSearch
    {
        // ---------------- DTOs & Params ----------------

        /// <summary>
        /// A single item from a ValueSet expansion: the code system, the code, and an optional display.
        /// </summary>
        /// <param name="System">The code system URI (e.g., <c>http://loinc.org</c>).</param>
        /// <param name="Code">The code within the system.</param>
        /// <param name="Display">Optional human-readable display string.</param>
        public sealed record ValueSetItem(string System, string Code, string? Display);

        /// <summary>
        /// Encapsulates parameters for a FHIR ValueSet <c>$expand</c> operation.
        /// </summary>
        public sealed class ValueSetSearchParams
        {
            /// <summary>
            /// The canonical URL for the ValueSet to expand.
            /// This identifies the ValueSet definition on the terminology server.
            /// <para><b>Required.</b></para>
            /// </summary>
            public string CanonicalUrl { get; init; } = default!;

            /// <summary>
            /// The publisher associated with the ValueSet expansion.
            /// Some servers require this to scope expansions or enforce organizational context.
            /// <para><b>Required.</b></para>
            /// </summary>
            public string Publisher { get; init; } = default!;

            /// <summary>
            /// Optional language code (e.g., "en", "fr") to localize display text in the expansion.
            /// </summary>
            public string? _language { get; init; }

            /// <summary>
            /// Optional activity category code used to filter or contextualize the expansion.
            /// Commonly applied for workflow-specific ValueSets.
            /// </summary>
            public string? activityCategoryCode { get; init; }

            /// <summary>
            /// Optional name filter for the ValueSet or its concepts.
            /// Useful for narrowing expansions by display name.
            /// </summary>
            public string? name { get; init; }

            /// <summary>
            /// Optional organization key to scope the expansion to a specific organizational context.
            /// </summary>
            public string? organizationKey { get; init; }

            /// <summary>
            /// Optional title filter for the ValueSet or its concepts.
            /// </summary>
            public string? title { get; init; }

            /// <summary>
            /// Optional URL override for the ValueSet.
            /// Typically not needed if <see cref="CanonicalUrl"/> is provided.
            /// </summary>
            public string? url { get; init; }

            /// <summary>
            /// Maximum number of items to return client-side.
            /// Defaults to <c>SearchExecutor.DefaultServerMaxResults</c> (500).
            /// Use this to defensively limit lists on large expansions.
            /// </summary>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;
        }
        /// <summary>
        /// Provides operations to expand ValueSets using ClientConfigurator and Builder&lt;T&gt;.
        /// Uses the FHIR $expand terminology operation with canonical URL and publisher.
        /// </summary>
        public static class ValueSetExpand
        {
            /// <summary>
            /// Executes a ValueSet $expand using the provided parameter bag.
            /// Builds query parameters via Builder&lt;T&gt; and invokes the type-level operation.
            /// CanonicalUrl and Publisher are required.
            /// </summary>
            /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
            /// <param name="p">
            /// Parameter bag with the canonical url (required), publisher (required),
            /// optional contextual parameters and a client-side defensive limit.
            /// </param>
            /// <param name="ct">Cancellation token.</param>
            /// <returns>The expanded ValueSet (with .Expansion populated). May be trimmed client-side to ListReturnLimit.</returns>
            public static async Task<ValueSet> ExpandAsync(
                ClientConfigurator configurator,
                ValueSetSearchParams p,
                CancellationToken ct = default)
            {
                ArgumentNullException.ThrowIfNull(configurator);
                ArgumentNullException.ThrowIfNull(p);

                // Enforce required params
                if (string.IsNullOrWhiteSpace(p.CanonicalUrl))
                    throw new ArgumentException("CanonicalUrl is required and must be a canonical ValueSet URL.", nameof(p));

                if (string.IsNullOrWhiteSpace(p.Publisher))
                    throw new ArgumentException("Publisher is required and cannot be null or empty.", nameof(p));

                var vsClient = configurator.FhirClient;

                // Use the same builder pattern as the template to construct operation parameters (no includes).
                var builder = new Builder<ValueSet>();

                // Required $expand parameters
                builder.With("url", p.CanonicalUrl);   // canonical URL for the ValueSet
                builder.With("publisher", p.Publisher);

                // Optional context/scoping parameters your server may honor
                if (!string.IsNullOrWhiteSpace(p._language)) builder.With("_language", p._language);
                if (!string.IsNullOrWhiteSpace(p.activityCategoryCode)) builder.With("activityCategoryCode", p.activityCategoryCode);
                if (!string.IsNullOrWhiteSpace(p.name)) builder.With("name", p.name);
                if (!string.IsNullOrWhiteSpace(p.organizationKey)) builder.With("organizationKey", p.organizationKey);
                if (!string.IsNullOrWhiteSpace(p.title)) builder.With("title", p.title);

                // NOTE:
                // - We intentionally do NOT send _include or any includes; $expand does not take includes.
                // - We do NOT send a server-side count here; we'll trim client-side via ListReturnLimit to match your pattern.

                // Build a SearchParams-like container, then translate to a Parameters resource for a robust POST $expand.
                var searchParams = builder.Build();
                var parms = new Parameters();

                // Convert all query pairs into Parameters. This keeps the "builder" pattern consistent with your template.
                foreach (var kvp in searchParams.ToUriParamList())
                {
                    // The $expand operation expects primitive parameters; represent as FHIR strings.
                    parms.Add(kvp.Item1, new FhirString(kvp.Item2));
                }

                // Invoke the terminology operation at the type level: [base]/ValueSet/$expand
                // We prefer POST (useGet: false) to avoid URL length limits and to standardize payload handling.
                var expandedResource = await vsClient.TypeOperationAsync<ValueSet>(
                    operationName: "expand",
                    parameters: parms,
                    useGet: true
                ).ConfigureAwait(false);


                var bundle = expandedResource as Bundle
                    ?? throw new InvalidOperationException("The $expand operation did not return a ValueSet resource.");

                var expanded = bundle.Entry.First().Resource as ValueSet;

                // Defensive client-side trim to ListReturnLimit (if present)
                var limit = SearchExecutor.NormalizeLimit(p.ListReturnLimit);
                if (expanded?.Expansion?.Contains != null && expanded.Expansion.Contains.Count > limit)
                {
                    expanded.Expansion.Contains = expanded.Expansion.Contains.Take(limit).ToList();
                }

                return expanded ?? throw new InvalidOperationException(
                    "The $expand operation returned a Bundle entry with a null ValueSet resource.");
            }

            // -----------------------------
            // Strongly typed convenience methods
            // -----------------------------

            /// <summary>
            /// Expands a ValueSet using an ARIA logical name and the canonical mapping table.
            /// Publisher is required.
            /// </summary>
            /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
            /// <param name="logicalValueSet">Aria logical value set identifier.</param>
            /// <param name="publisher">Publisher for the ValueSet.</param>
            /// <param name="listReturnLimit">Maximum number of items to return.</param>
            /// <param name="language">Optional language code.</param>
            /// <param name="organizationKey">Optional organization key.</param>
            /// <param name="activityCategoryCode">Optional activity category code.</param>
            /// <param name="name">Optional name filter.</param>
            /// <param name="title">Optional title filter.</param>
            /// <param name="ct">Cancellation token.</param>
            public static Task<ValueSet> ExpandAsync(
                ClientConfigurator configurator,
                AriaValueSet logicalValueSet,
                string publisher,
                int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
                string? language = null,
                string? organizationKey = null,
                string? activityCategoryCode = null,
                string? name = null,
                string? title = null,
                CancellationToken ct = default)
            {
                if (!SearchTypes.AriaCanonicalMap.TryGetValue(logicalValueSet, out var canonical))
                    throw new KeyNotFoundException($"No canonical mapping found for {logicalValueSet}.");

                var p = new ValueSetSearchParams
                {
                    CanonicalUrl = canonical,
                    Publisher = publisher,
                    _language = language,
                    organizationKey = organizationKey,
                    activityCategoryCode = activityCategoryCode,
                    name = name,
                    title = title,
                    ListReturnLimit = listReturnLimit
                };

                return ExpandAsync(configurator, p, ct);
            }

            /// <summary>
            /// Expands a ValueSet using a raw canonical URL. Publisher is required.
            /// </summary>
            /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
            /// <param name="canonicalUrl">Canonical URL of the ValueSet.</param>
            /// <param name="publisher">Publisher for the ValueSet.</param>
            /// <param name="listReturnLimit">Maximum number of items to return.</param>
            /// <param name="language">Optional language code.</param>
            /// <param name="organizationKey">Optional organization key.</param>
            /// <param name="activityCategoryCode">Optional activity category code.</param>
            /// <param name="name">Optional name filter.</param>
            /// <param name="title">Optional title filter.</param>
            /// <param name="ct">Cancellation token.</param>
            public static Task<ValueSet> ExpandByCanonicalAsync(
                ClientConfigurator configurator,
                string canonicalUrl,
                string publisher,
                int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
                string? language = null,
                string? organizationKey = null,
                string? activityCategoryCode = null,
                string? name = null,
                string? title = null,
                CancellationToken ct = default)
            {
                var p = new ValueSetSearchParams
                {
                    CanonicalUrl = canonicalUrl,
                    Publisher = publisher,
                    _language = language,
                    organizationKey = organizationKey,
                    activityCategoryCode = activityCategoryCode,
                    name = name,
                    title = title,
                    ListReturnLimit = listReturnLimit
                };

                return ExpandAsync(configurator, p, ct);
            }

            /// <summary>
            /// Convenience: expand DiagnosisCode with optional activity/category context.
            /// </summary>
            /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
            /// <param name="publisher">Publisher for the ValueSet.</param>
            /// <param name="activityCategoryCode">Optional activity category code.</param>
            /// <param name="listReturnLimit">Maximum number of items to return.</param>
            /// <param name="ct">Cancellation token.</param>
            public static Task<ValueSet> ExpandDiagnosisCodesAsync(
                ClientConfigurator configurator,
                string publisher,
                string? activityCategoryCode = null,
                int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
                CancellationToken ct = default)
            {
                return ExpandAsync(
                    configurator,
                    AriaValueSet.DiagnosisCode,
                    publisher: publisher,
                    listReturnLimit: listReturnLimit,
                    activityCategoryCode: activityCategoryCode,
                    ct: ct
                );
            }
        }
    }
}


