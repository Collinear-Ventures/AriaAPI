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

namespace AriaAPI.API.SingleResourceSearch
{
    /// <summary>
    /// Provides search operations for FHIR <see cref="PractitionerRole"/> resources using
    /// <see cref="ClientConfigurator"/> and <c>Builder&lt;T&gt;</c>.
    /// </summary>
    /// <remarks>
    /// Supported search parameters:
    /// <list type="table">
    ///   <listheader>
    ///     <term>Parameter</term>
    ///     <description>Description</description>
    ///   </listheader>
    ///   <item><term><c>_id</c></term><description>Logical ID of the resource.</description></item>
    ///   <item><term><c>practitioner</c></term><description>Practitioner reference.</description></item>
    ///   <item><term><c>organization</c></term><description>Organization reference.</description></item>
    ///   <item><term><c>location</c></term><description>Location reference.</description></item>
    ///   <item><term><c>specialty</c></term><description>Specific specialty (token).</description></item>
    ///   <item><term><c>role</c></term><description>The practitioner can perform this role (token).</description></item>
    ///   <item><term><c>active</c></term><description>Whether the practitioner role record is in active use.</description></item>
    /// </list>
    /// Includes are supported via <see cref="PractitionerRoleInclude"/> and default to <see cref="PractitionerRoleInclude.Practitioner"/>.
    /// </remarks>
    public static class PractitionerRoleSearch
    {
        /// <summary>
        /// Encapsulates search parameters for <see cref="PractitionerRole"/> queries.
        /// </summary>
        public sealed class PractitionerRoleSearchParams
        {
            /// <summary>Logical ID of the PractitionerRole resource (FHIR <c>_id</c>).</summary>
            public string? Id { get; init; }

            /// <summary>Practitioner reference or id.</summary>
            public string? Practitioner { get; init; }

            /// <summary>Organization reference or id.</summary>
            public string? Organization { get; init; }

            /// <summary>Location reference or id.</summary>
            public string? Location { get; init; }

            /// <summary>Specific specialty token (e.g., <c>394814009</c>).</summary>
            public string? Specialty { get; init; }

            /// <summary>The practitioner can perform this role (token).</summary>
            public string? Role { get; init; }

            /// <summary>Whether the practitioner role record is in active use.</summary>
            public bool? Active { get; init; }

            /// <summary>
            /// FHIR _include paths for related resources.
            /// If null, defaults to including <see cref="PractitionerRoleInclude.Practitioner"/>.
            /// </summary>
            public IEnumerable<PractitionerRoleInclude>? Includes { get; init; }

            /// <summary>Apply <c>:iterate</c> modifier to includes if supported by the server.</summary>
            public bool UseIterateModifier { get; init; } = false;

            /// <summary>
            /// Maximum number of PractitionerRole resources to return client-side.
            /// Defaults to <c>SearchExecutor.DefaultServerMaxResults</c> (500).
            /// </summary>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;
        }

        /// <summary>
        /// Executes a flexible PractitionerRole search using the provided parameter bag.
        /// Builds FHIR <see cref="SearchParams"/> via <c>Builder&lt;T&gt;</c> and aggregates results across pages.
        /// </summary>
        /// <param name="configurator">The <see cref="ClientConfigurator"/> used to create a resource-specific client.</param>
        /// <param name="p">Search parameter bag.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A deduplicated list of <see cref="PractitionerRole"/> resources matching the criteria.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="configurator"/> is <c>null</c>.</exception>
        public static async Task<List<PractitionerRole>> SearchPractitionerRolesAsync(
            ClientConfigurator configurator,
            PractitionerRoleSearchParams p,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new PractitionerRoleSearchParams();

            var limit = SearchExecutor.NormalizeLimit(p.ListReturnLimit);
            var modifier = p.UseIterateModifier ? IncludeModifier.Iterate : IncludeModifier.None;

            Builder<PractitionerRole> MakeBaseBuilder()
            {
                var builder = new Builder<PractitionerRole>();

                if (!string.IsNullOrWhiteSpace(p.Id))
                    builder.With("_id", p.Id);
                if (!string.IsNullOrWhiteSpace(p.Practitioner))
                    builder.With("practitioner", p.Practitioner);
                if (!string.IsNullOrWhiteSpace(p.Organization))
                    builder.With("organization", p.Organization);
                if (!string.IsNullOrWhiteSpace(p.Location))
                    builder.With("location", p.Location);
                if (!string.IsNullOrWhiteSpace(p.Specialty))
                    builder.With("specialty", p.Specialty);
                if (!string.IsNullOrWhiteSpace(p.Role))
                    builder.With("role", p.Role);
                if (p.Active.HasValue)
                    builder.With("active", p.Active.Value ? "true" : "false");

                if (p.Includes is not null && p.Includes.Any())
                    builder.Include(p.Includes, modifier: modifier);
                else
                    builder.Include(PractitionerRoleInclude.Practitioner);

                if (limit != int.MaxValue)
                    builder.WithCount(limit);
                return builder;
            }

            return await SearchExecutor.ExecuteAsync(
                configurator,
                MakeBaseBuilder,
                null,
                limit,
                ct).ConfigureAwait(false);
        }

        // -----------------------------
        // Strongly-typed convenience methods
        // -----------------------------

        /// <summary>
        /// Returns practitioner roles for a specific practitioner.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="practitioner">Practitioner reference or id.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<PractitionerRole>> ByPractitionerAsync(
            ClientConfigurator configurator,
            string practitioner,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<PractitionerRoleInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new PractitionerRoleSearchParams
            {
                Practitioner = practitioner,
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchPractitionerRolesAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns a single practitioner role by its logical id.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="id">Logical id of the resource.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<PractitionerRole>> ByIdAsync(
            ClientConfigurator configurator,
            string id,
            IEnumerable<PractitionerRoleInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new PractitionerRoleSearchParams
            {
                Id = id,
                ListReturnLimit = 1,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchPractitionerRolesAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns practitioner roles for a specific organization.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="organization">Organization reference or id.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<PractitionerRole>> ByOrganizationAsync(
            ClientConfigurator configurator,
            string organization,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<PractitionerRoleInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new PractitionerRoleSearchParams
            {
                Organization = organization,
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchPractitionerRolesAsync(configurator, p, ct);
        }
    }
}
