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
    /// Provides search operations for FHIR <see cref="HealthcareService"/> resources using
    /// <see cref="ClientConfigurator"/> and <c>Builder&lt;T&gt;</c>, with optional FHIR <c>_include</c> support.
    ///
    /// Supported search parameters:
    /// <list type="table">
    ///   <listheader>
    ///     <term>Parameter</term>
    ///     <description>Description</description>
    ///   </listheader>
    ///   <item><term><c>_id</c></term><description>Logical ID of the resource.</description></item>
    ///   <item><term><c>active</c></term><description>Active flag (true | false).</description></item>
    ///   <item><term><c>identifier</c></term><description>External identifier (token). Accepts value-only or <c>system|value</c>.</description></item>
    ///   <item><term><c>name</c></term><description>Portion of the healthcare service name.</description></item>
    ///   <item><term><c>organization</c></term><description>Organization that provides this healthcare service.</description></item>
    /// </list>
    ///
    /// Supported includes:
    /// <list type="bullet">
    ///   <item><description><see cref="HealthcareServiceInclude.Organization"/> (FHIR: <c>HealthcareService:organization</c>)</description></item>
    ///   <item><description>Optionally, <see cref="OrganizationInclude"/> values when enriching the included Organization (e.g., <c>Organization:partof</c>).</description></item>
    /// </list>
    /// </summary>
    public static class HealthcareServiceSearch
    {
        /// <summary>
        /// Encapsulates search parameters for HealthcareService queries.
        /// All parameters are optional; supply any combination to constrain results.
        /// </summary>
        public sealed class HealthcareServiceSearchParams
        {
            /// <summary>
            /// Logical ID of the HealthcareService resource (FHIR <c>_id</c>).
            /// </summary>
            public string? Id { get; init; }

            /// <summary>
            /// Active flag (FHIR search parameter <c>active</c>).
            /// </summary>
            public bool? Active { get; init; }

            /// <summary>
            /// External identifier (FHIR token search <c>identifier</c>).
            /// Accepts value-only or <c>system|value</c> form depending on server support.
            /// </summary>
            public string? Identifier { get; init; }

            /// <summary>
            /// Portion of the healthcare service name (FHIR search parameter <c>name</c>).
            /// </summary>
            public string? Name { get; init; }

            /// <summary>
            /// Organization that provides this healthcare service (FHIR search parameter <c>organization</c>).
            /// </summary>
            public string? Organization { get; init; }

            /// <summary>
            /// Maximum number of HealthcareService resources to return client-side.
            /// Defaults to <c>SearchExecutor.DefaultServerMaxResults</c> (500).
            /// </summary>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;

            /// <summary>
            /// Includes to apply on Organization (when you want to enrich the included Organization, e.g., <see cref="OrganizationInclude.PartOf"/>).
            /// </summary>
            public OrganizationInclude[]? IncludeOrganization { get; init; }
        }

        /// <summary>
        /// Executes a HealthcareService search using the provided parameter bag.
        /// Builds FHIR <see cref="SearchParams"/> via <c>Builder&lt;T&gt;</c>, applies optional <c>_include</c>s,
        /// and aggregates results across pages.
        /// </summary>
        /// <param name="configurator">The <see cref="ClientConfigurator"/> used to create a resource-specific client.</param>
        /// <param name="p">Search parameter bag containing _id, active, identifier, name, organization, includes, and limits.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A list of <see cref="HealthcareService"/> resources matching the criteria.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="configurator"/> is <c>null</c>.</exception>
        public static async Task<List<HealthcareService>> SearchHealthcareServicesAsync(
            ClientConfigurator configurator,
            HealthcareServiceSearchParams p,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new HealthcareServiceSearchParams();

            var limit = SearchExecutor.NormalizeLimit(p.ListReturnLimit);

            return await SearchExecutor.ExecuteAsync(
                configurator,
                () =>
                {
                    var builder = new Builder<HealthcareService>();

                    if (!string.IsNullOrWhiteSpace(p.Id))
                        builder.With("_id", p.Id);

                    if (p.Active.HasValue)
                        builder.With("active", p.Active.Value ? "true" : "false");

                    if (!string.IsNullOrWhiteSpace(p.Identifier))
                        builder.With("identifier", p.Identifier);

                    if (!string.IsNullOrWhiteSpace(p.Name))
                        builder.With("name", p.Name);

                    if (!string.IsNullOrWhiteSpace(p.Organization))
                        builder.With("organization", p.Organization);

                    if (limit != int.MaxValue)
                        builder.WithCount(limit);

                    // Organization-level includes (optional; for enriching the included Organization)
                    if (p.IncludeOrganization is { Length: > 0 })
                    {
                        builder.Include(p.IncludeOrganization, modifier: IncludeModifier.None);
                    }

                    return builder;
                },
                null,
                limit,
                ct).ConfigureAwait(false);
        }

        // -----------------------------
        // Strongly typed convenience methods
        // -----------------------------

        /// <summary>
        /// Returns HealthcareServices by resource ID.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="id">Logical ID of the resource.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<HealthcareService>> ByIdAsync(
            ClientConfigurator configurator,
            string id,
            int listReturnLimit = int.MaxValue,
            CancellationToken ct = default)
        {
            var p = new HealthcareServiceSearchParams
            {
                Id = id,
                ListReturnLimit = listReturnLimit
            };
            return SearchHealthcareServicesAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns HealthcareServices by name (partial match).
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="name">Name to search for.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<HealthcareService>> ByNameAsync(
            ClientConfigurator configurator,
            string name,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new HealthcareServiceSearchParams
            {
                Name = name,
                ListReturnLimit = listReturnLimit
            };
            return SearchHealthcareServicesAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns HealthcareServices by active status.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="active">Active status to filter by.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<HealthcareService>> ByActiveAsync(
            ClientConfigurator configurator,
            bool active,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new HealthcareServiceSearchParams
            {
                Active = active,
                ListReturnLimit = listReturnLimit
            };
            return SearchHealthcareServicesAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns HealthcareServices by identifier (token).
        /// Accepts value-only or <c>system|value</c>.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="identifier">Identifier to filter by.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<HealthcareService>> ByIdentifierAsync(
            ClientConfigurator configurator,
            string identifier,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new HealthcareServiceSearchParams
            {
                Identifier = identifier,
                ListReturnLimit = listReturnLimit
            };
            return SearchHealthcareServicesAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns HealthcareServices by providing organization.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="organization">Organization reference or id.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includeHealthcareService">Optional HealthcareService includes.</param>
        /// <param name="includeOrganization">Optional Organization includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<HealthcareService>> ByOrganizationAsync(
            ClientConfigurator configurator,
            string organization,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            HealthcareServiceInclude[]? includeHealthcareService = null,
            OrganizationInclude[]? includeOrganization = null,
            CancellationToken ct = default)
        {
            var p = new HealthcareServiceSearchParams
            {
                Organization = organization,
                ListReturnLimit = listReturnLimit,
                IncludeOrganization = includeOrganization
            };
            return SearchHealthcareServicesAsync(configurator, p, ct);
        }
    }
}
