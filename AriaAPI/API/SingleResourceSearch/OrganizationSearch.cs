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

namespace AriaAPI.API.SingleResourceSearch
{
    /// <summary>
    /// Provides search operations for FHIR <see cref="Organization"/> resources using
    /// <see cref="ClientConfigurator"/> and <c>Builder&lt;T&gt;</c>.
    /// </summary>
    /// <remarks>
    /// Supported search parameters:
    /// _id, active, identifier, name, partof, type
    /// Includes: _include=Organization:partof
    /// </remarks>
    public static class OrganizationSearch
    {
        /// <summary>
        /// Encapsulates search parameters for Organization queries.
        /// </summary>
        public sealed class OrganizationSearchParams
        {
            /// <summary>Logical ID of the Organization resource.</summary>
            public string? Id { get; init; }

            /// <summary>Active flag.</summary>
            public bool? Active { get; init; }

            /// <summary>External identifier.</summary>
            public string? Identifier { get; init; }

            /// <summary>Organization name.</summary>
            public string? Name { get; init; }

            /// <summary>Parent organization reference.</summary>
            public string? PartOf { get; init; }

            /// <summary>Organization type token.</summary>
            public string? Type { get; init; }

            /// <summary>Include Organization:partof in the search.</summary>
            public bool IncludePartOf { get; init; } = false;

            /// <summary>Client-side defensive cap. Defaults to <c>SearchExecutor.DefaultServerMaxResults</c> (500).</summary>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;
        }

        /// <summary>
        /// Executes an Organization search using the provided parameter bag.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="p">Search parameter bag.</param>
        /// <param name="ct">Cancellation token.</param>
        public static async Task<List<Organization>> SearchOrganizationsAsync(
            ClientConfigurator configurator,
            OrganizationSearchParams p,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new OrganizationSearchParams();

            var limit = SearchExecutor.NormalizeLimit(p.ListReturnLimit);

            return await SearchExecutor.ExecuteAsync(
                configurator,
                () =>
                {
                    var builder = new Builder<Organization>();

                    if (!string.IsNullOrWhiteSpace(p.Id))
                        builder.With("_id", p.Id);

                    if (p.Active.HasValue)
                        builder.With("active", p.Active.Value ? "true" : "false");

                    if (!string.IsNullOrWhiteSpace(p.Identifier))
                        builder.With("identifier", p.Identifier);

                    if (!string.IsNullOrWhiteSpace(p.Name))
                        builder.With("name", p.Name);

                    if (!string.IsNullOrWhiteSpace(p.PartOf))
                        builder.With("partof", p.PartOf);

                    if (!string.IsNullOrWhiteSpace(p.Type))
                        builder.With("type", p.Type);

                    if (limit != int.MaxValue)
                        builder.WithCount(limit);

                    // Add include if requested
                    if (p.IncludePartOf)
                    {
                        builder.Include(OrganizationInclude.PartOf);
                    }

                    return builder;
                },
                null,
                limit,
                ct).ConfigureAwait(false);
        }

        // -----------------------------
        // Convenience methods
        // -----------------------------

        /// <summary>
        /// Returns Organizations by resource ID.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="id">Logical ID of the resource.</param>
        /// <param name="includePartOf">Whether to include partof references.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Organization>> ByIdAsync(
            ClientConfigurator configurator,
            string id,
            bool includePartOf = false,
            int listReturnLimit = int.MaxValue,
            CancellationToken ct = default)
        {
            var p = new OrganizationSearchParams
            {
                Id = id,
                IncludePartOf = includePartOf,
                ListReturnLimit = listReturnLimit
            };
            return SearchOrganizationsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns Organizations by name.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="name">Name to search for.</param>
        /// <param name="includePartOf">Whether to include partof references.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Organization>> ByNameAsync(
            ClientConfigurator configurator,
            string name,
            bool includePartOf = false,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new OrganizationSearchParams
            {
                Name = name,
                IncludePartOf = includePartOf,
                ListReturnLimit = listReturnLimit
            };
            return SearchOrganizationsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns Organizations by active status.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="active">Active status to filter by.</param>
        /// <param name="includePartOf">Whether to include partof references.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Organization>> ByActiveAsync(
            ClientConfigurator configurator,
            bool active,
            bool includePartOf = false,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new OrganizationSearchParams
            {
                Active = active,
                IncludePartOf = includePartOf,
                ListReturnLimit = listReturnLimit
            };
            return SearchOrganizationsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns Organizations by identifier (token).
        /// Supports value-only or system|value forms depending on server support.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="identifierToken">Identifier token to filter by.</param>
        /// <param name="includePartOf">Whether to include partof references.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Organization>> ByIdentifierAsync(
            ClientConfigurator configurator,
            string identifierToken,
            bool includePartOf = false,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new OrganizationSearchParams
            {
                Identifier = identifierToken,
                IncludePartOf = includePartOf,
                ListReturnLimit = listReturnLimit
            };
            return SearchOrganizationsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns Organizations by type (token).
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="typeToken">Type token to filter by.</param>
        /// <param name="includePartOf">Whether to include partof references.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Organization>> ByTypeAsync(
            ClientConfigurator configurator,
            string typeToken,
            bool includePartOf = false,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new OrganizationSearchParams
            {
                Type = typeToken,
                IncludePartOf = includePartOf,
                ListReturnLimit = listReturnLimit
            };
            return SearchOrganizationsAsync(configurator, p, ct);
        }
    }
}
