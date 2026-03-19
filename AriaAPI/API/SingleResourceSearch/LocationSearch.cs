// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.API.SearchHelpers;
using AriaAPI.Core;
using AriaAPI.Networking.Core;
using AriaAPI.Resources.Includes;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static AriaAPI.API.SearchHelpers.SearchHelpers;
using static AriaAPI.API.SearchHelpers.SearchTypes;

namespace AriaAPI.API.SingleResourceSearch
{
    /// <summary>
    /// Provides search operations for FHIR <see cref="Location"/> resources using
    /// <see cref="ClientConfigurator"/> and <c>Builder&lt;T&gt;</c>, with an IncludeConflictPolicy
    /// that can fall back to two-pass enrichment when <c>name + _include</c> is rejected by the server.
    /// </summary>
    public static class LocationSearch
    {
        /// <summary>
        /// Encapsulates search parameters for Location queries.
        /// All parameters are optional; supply any combination to constrain results.
        /// </summary>
        public sealed class LocationSearchParams
        {
            /// <summary>
            /// Logical ID of the Location resource (FHIR <c>_id</c>).
            /// </summary>
            public string? Id { get; init; }

            /// <summary>
            /// Name of the location (FHIR search parameter <c>name</c>).
            /// </summary>
            public string? Name { get; init; }

            /// <summary>
            /// External identifier for the location (FHIR token search <c>identifier</c>).
            /// Accepts value-only or <c>system|value</c> form depending on server support.
            /// </summary>
            public string? Identifier { get; init; }

            /// <summary>
            /// Organization responsible for the location (FHIR search parameter <c>service-organization</c>).
            /// </summary>
            public string? ServiceOrganization { get; init; }

            /// <summary>
            /// Status of the location (FHIR <c>status</c>; <c>active</c> | <c>inactive</c>).
            /// </summary>
            public LocationStatus? Status { get; init; }

            /// <summary>
            /// Location type (FHIR <c>type</c>; <c>auxiliary</c> | <c>venue</c>).
            /// </summary>
            public LocationType? Type { get; init; }

            /// <summary>
            /// When <c>true</c>, we either request <c>_include=Location:service-organization</c> (first-pass),
            /// or, if suppressed by policy, we enrich in a second pass by fetching Organizations and adding them to <c>Contained</c>.
            /// </summary>
            public bool IncludeServiceOrganization { get; init; } = false;

            /// <summary>
            /// How to handle servers that reject <c>name</c> + <c>_include</c>:
            /// <list type="bullet">
            /// <item><description><see cref="IncludeConflictPolicy.SuppressIncludes"/>: Omit the <c>_include</c> in the initial query when conflict is detected.</description></item>
            /// <item><description><see cref="IncludeConflictPolicy.EnrichInSecondPass"/>: Perform a second pass to fetch related Organizations and add them to <c>Contained</c>.</description></item>
            /// </list>
            /// Default is <see cref="IncludeConflictPolicy.EnrichInSecondPass"/>.
            /// </summary>
            public IncludeConflictPolicy IncludePolicy { get; init; } = IncludeConflictPolicy.EnrichInSecondPass;

            /// <summary>
            /// Maximum number of Location resources to return client-side (defensive trim).
            /// Defaults to <c>SearchExecutor.DefaultServerMaxResults</c> (500).
            /// </summary>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;
        }

        /// <summary>
        /// Executes a Location search using the provided parameter bag.
        /// Supports two-pass enrichment for <c>service-organization</c> when requested and policy = <c>EnrichInSecondPass</c>.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="p">Search parameter bag.</param>
        /// <param name="ct">Cancellation token.</param>
        public static async Task<List<Location>> SearchLocationsAsync(
            ClientConfigurator configurator,
            LocationSearchParams p,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new LocationSearchParams();

            var limit = SearchExecutor.NormalizeLimit(p.ListReturnLimit);

            bool includeRequested = p.IncludeServiceOrganization;
            bool includeConflictLikely = includeRequested && !string.IsNullOrWhiteSpace(p.Name); // mirror: name + _include can be rejected
            bool shouldIncludeInFirstPass = (includeRequested && includeConflictLikely) && (includeRequested && p.IncludePolicy != IncludeConflictPolicy.SuppressIncludes);

            var results = await SearchExecutor.ExecuteAsync(
                configurator,
                () =>
                {
                    var builder = new Builder<Location>();

                    if (!string.IsNullOrWhiteSpace(p.Id))
                        builder.With("_id", p.Id);

                    if (!string.IsNullOrWhiteSpace(p.Name))
                        builder.With("name", p.Name);

                    if (!string.IsNullOrWhiteSpace(p.Identifier))
                        builder.With("identifier", p.Identifier);

                    if (!string.IsNullOrWhiteSpace(p.ServiceOrganization))
                        builder.With("service-organization", p.ServiceOrganization);

                    if (p.Status.HasValue)
                        builder.With("status", StatusToToken(p.Status.Value));

                    if (p.Type.HasValue)
                        builder.With("type", TypeToToken(p.Type.Value));

                    if (limit > 0 && limit < int.MaxValue)
                        builder.WithCount(limit);

                    if (shouldIncludeInFirstPass)
                    {
                        // _include=Location:service-organization
                        builder.Include(LocationInclude.ServiceOrganization);
                    }

                    return builder;
                },
                null,
                limit,
                ct).ConfigureAwait(false);

            // ------------- Second pass enrichment (optional) -------------
            // When includes were requested but suppressed (or when caller prefers enrichment),
            // fetch related Organization resources referenced by Location.ManagingOrganization
            // and add them to Location.Contained for each Location.
            if (includeRequested && p.IncludePolicy == IncludeConflictPolicy.EnrichInSecondPass)
            {
                await EnrichServiceOrganizationsAsync(results, configurator, ct).ConfigureAwait(false);
            }

            return results;
        }

        // -----------------------------
        // Strongly typed convenience methods
        // -----------------------------

        /// <summary>
        /// Returns Locations by resource ID.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="id">Logical ID of the resource.</param>
        /// <param name="includeServiceOrganization">Whether to include the service organization.</param>
        /// <param name="includePolicy">Policy for handling include conflicts.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Location>> ByIdAsync(
            ClientConfigurator configurator,
            string id,
            bool includeServiceOrganization = false,
            IncludeConflictPolicy includePolicy = IncludeConflictPolicy.EnrichInSecondPass,
            int listReturnLimit = int.MaxValue,
            CancellationToken ct = default)
        {
            var p = new LocationSearchParams
            {
                Id = id,
                IncludeServiceOrganization = includeServiceOrganization,
                IncludePolicy = includePolicy,
                ListReturnLimit = listReturnLimit
            };
            return SearchLocationsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns Locations by name.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="name">Name to search for.</param>
        /// <param name="includeServiceOrganization">Whether to include the service organization.</param>
        /// <param name="includePolicy">Policy for handling include conflicts.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Location>> ByNameAsync(
            ClientConfigurator configurator,
            string name,
            bool includeServiceOrganization = false,
            IncludeConflictPolicy includePolicy = IncludeConflictPolicy.EnrichInSecondPass,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new LocationSearchParams
            {
                Name = name,
                IncludeServiceOrganization = includeServiceOrganization,
                IncludePolicy = includePolicy,
                ListReturnLimit = listReturnLimit
            };
            return SearchLocationsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns Locations by status.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="status">Status to filter by.</param>
        /// <param name="includeServiceOrganization">Whether to include the service organization.</param>
        /// <param name="includePolicy">Policy for handling include conflicts.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Location>> ByStatusAsync(
            ClientConfigurator configurator,
            LocationStatus status,
            bool includeServiceOrganization = false,
            IncludeConflictPolicy includePolicy = IncludeConflictPolicy.EnrichInSecondPass,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new LocationSearchParams
            {
                Status = status,
                IncludeServiceOrganization = includeServiceOrganization,
                IncludePolicy = includePolicy,
                ListReturnLimit = listReturnLimit
            };
            return SearchLocationsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns Locations by type.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="type">Location type to filter by.</param>
        /// <param name="includeServiceOrganization">Whether to include the service organization.</param>
        /// <param name="includePolicy">Policy for handling include conflicts.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Location>> ByTypeAsync(
            ClientConfigurator configurator,
            LocationType type,
            bool includeServiceOrganization = false,
            IncludeConflictPolicy includePolicy = IncludeConflictPolicy.EnrichInSecondPass,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new LocationSearchParams
            {
                Type = type,
                IncludeServiceOrganization = includeServiceOrganization,
                IncludePolicy = includePolicy,
                ListReturnLimit = listReturnLimit
            };
            return SearchLocationsAsync(configurator, p, ct);
        }

        // -----------------------------
        // Second-pass enrichment helpers
        // -----------------------------

        /// <summary>
        /// Fetches Organizations referenced by <see cref="Location.ManagingOrganization"/> and adds
        /// them to <see cref="DomainResource.Contained"/> for each referencing Location (if not already present).
        /// </summary>
        private static async System.Threading.Tasks.Task EnrichServiceOrganizationsAsync(
            List<Location> locations,
            ClientConfigurator configurator,
            CancellationToken ct = default)
        {
            if (locations == null || locations.Count == 0) return;

            // Gather distinct Organization references
            var orgRefs = locations
                .Select(l => l?.ManagingOrganization?.Reference) // supports "Organization/{id}" or absolute
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (orgRefs.Count == 0) return;

            var orgClient = configurator.ForResource<Organization>(ct);
            var cache = new Dictionary<string, Organization>(StringComparer.OrdinalIgnoreCase);

            foreach (var orgRef in orgRefs)
            {
                try
                {
                    // Avoid duplicate calls
                    if (cache.ContainsKey(orgRef!)) continue;

                    var org = await orgClient.ReadAsync(orgRef!, null).ConfigureAwait(false);
                    if (org != null)
                    {
                        cache[orgRef!] = org;
                    }
                }
                catch
                {
                    // log via telemetry pipeline
                }
            }

            if (cache.Count == 0) return;

            // Attach Organizations into Location.Contained where reference matches
            foreach (var loc in locations)
            {
                var refStr = loc?.ManagingOrganization?.Reference;
                if (string.IsNullOrWhiteSpace(refStr)) continue;

                if (!cache.TryGetValue(refStr!, out var org)) continue;

                if (loc!.Contained == null)
                    loc.Contained = new List<Resource>();

                // Add if not already present (by Id)
                var already = loc.Contained.OfType<Organization>()
                    .Any(o => !string.IsNullOrWhiteSpace(o.Id) && o.Id.Equals(org.Id, StringComparison.OrdinalIgnoreCase));

                if (!already)
                {
                    // Ensure the contained Organization has an Id for reference consistency
                    if (string.IsNullOrWhiteSpace(org.Id))
                        org.Id = ExtractOrganizationId(refStr!);

                    loc.Contained.Add(org);
                }
            }
        }

        private static string ExtractOrganizationId(string reference)
        {
            // Handles "Organization/{id}" and absolute ".../Organization/{id}"
            var parts = reference.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = parts.Length - 1; i >= 1; i--)
            {
                if (parts[i - 1].Equals("Organization", StringComparison.OrdinalIgnoreCase))
                    return parts[i];
            }
            return reference; // fallback (non-standard references)
        }
    }
}
