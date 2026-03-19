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
using static AriaAPI.API.SearchHelpers.SearchTypes;

namespace AriaAPI.API.SingleResourceSearch
{
    /// <summary>
    /// Provides search operations for FHIR <see cref="Practitioner"/> resources using
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
    ///   <item><term><c>active</c></term><description>Whether the practitioner record is active (true|false).</description></item>
    ///   <item><term><c>family</c></term><description>Family name of the practitioner.</description></item>
    ///   <item><term><c>given</c></term><description>Given name of the practitioner.</description></item>
    ///   <item><term><c>identifier</c></term><description>Identifier for the practitioner.</description></item>
    ///   <item><term><c>name</c></term><description>Name of the practitioner (string match).</description></item>
    ///   <item><term><c>practitioner-role</c></term><description>Role associated with the practitioner (enum or token).</description></item>
    ///   <item><term><c>service-organization</c></term><description>Organization providing the service.</description></item>
    /// </list>
    ///
    /// Includes:
    /// <list type="bullet">
    ///   <item><description><c>_include=Practitioner:service-organization</c></description></item>
    /// </list>
    /// </remarks>
    public static class PractitionerSearch
    {
        /// <summary>
        /// Encapsulates search parameters for Practitioner queries.
        /// All parameters are optional; supply any combination to constrain results.
        /// </summary>
        public sealed class PractitionerSearchParams
        {
            /// <summary>Logical ID of the Practitioner resource.</summary>
            public string? Id { get; init; }

            /// <summary>Whether the practitioner record is active.</summary>
            public bool? Active { get; init; }

            /// <summary>Family name of the practitioner.</summary>
            public string? Family { get; init; }

            /// <summary>Given name of the practitioner.</summary>
            public string? Given { get; init; }

            /// <summary>Identifier for the practitioner.</summary>
            public string? Identifier { get; init; }

            /// <summary>Name of the practitioner (string match).</summary>
            public string? Name { get; init; }

            /// <summary>
            /// Practitioner role (enum). If set, takes precedence over <see cref="PractitionerRoleToken"/>.
            /// </summary>
            public PractitionerRoleShort? PractitionerRole { get; init; }

            /// <summary>
            /// Practitioner role (free-text or system-specific token). Used if <see cref="PractitionerRole"/> is not set or is <c>Other</c>.
            /// </summary>
            public string? PractitionerRoleToken { get; init; }

            /// <summary>
            /// Organization filter. Supply either a reference (e.g. "Organization/123") or server-supported token.
            /// </summary>
            public string? ServiceOrganization { get; init; }

            /// <summary>Include Practitioner:service-organization.</summary>
            public bool IncludeServiceOrganization { get; init; } = false;

            /// <summary>Client-side defensive cap. Defaults to <c>SearchExecutor.DefaultServerMaxResults</c> (500).</summary>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;
        }

        /// <summary>
        /// Executes a Practitioner search using the provided parameter bag.
        /// Builds FHIR <see cref="SearchParams"/> via <c>Builder&lt;T&gt;</c> and aggregates results across pages.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="p">Search parameter bag.</param>
        /// <param name="ct">Cancellation token.</param>
        public static async Task<List<Practitioner>> SearchPractitionersAsync(
            ClientConfigurator configurator,
            PractitionerSearchParams p,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new PractitionerSearchParams();

            var limit = SearchExecutor.NormalizeLimit(p.ListReturnLimit);

            var results = await SearchExecutor.ExecuteAsync(
                configurator,
                () =>
                {
                    var builder = new Builder<Practitioner>();

                    if (!string.IsNullOrWhiteSpace(p.Id))
                        builder.With("_id", p.Id);

                    if (p.Active.HasValue)
                        builder.With("active", p.Active.Value.ToString().ToLowerInvariant());

                    if (!string.IsNullOrWhiteSpace(p.Family))
                        builder.With("family", p.Family);

                    if (!string.IsNullOrWhiteSpace(p.Given))
                        builder.With("given", p.Given);

                    if (!string.IsNullOrWhiteSpace(p.Identifier))
                        builder.With("identifier", p.Identifier);

                    if (!string.IsNullOrWhiteSpace(p.Name))
                        builder.With("name", p.Name);

                    var roleToken = ResolveRoleToken(p.PractitionerRole, p.PractitionerRoleToken);
                    if (!string.IsNullOrWhiteSpace(roleToken))
                        builder.With("practitioner-role", roleToken);

                    if (!string.IsNullOrWhiteSpace(p.ServiceOrganization))
                        builder.With("service-organization", p.ServiceOrganization);

                    if (limit > 0 && limit != int.MaxValue)
                        builder.WithCount(limit);

                    // Attempt _include if allowed
                    var resourceName = FhirIncludeResources.GetResourceNameFor<PractitionerInclude>();
                    //if (p.IncludeServiceOrganization)
                    //    builder.Include(resourceName, PractitionerInclude.ServiceOrganization);

                    return builder;
                },
                null,
                limit,
                ct).ConfigureAwait(false);

            // -----------------------------
            // Enrichment: Fetch Organization if _include didn't populate
            // -----------------------------
            if (p.IncludeServiceOrganization)
            {
                var orgClient = configurator.ForResource<Organization>(ct);

                foreach (var practitioner in results)
                {
                    // Check if organization is already included
                    bool hasOrg = practitioner.Contained?.OfType<Organization>().Any() ?? false;
                    if (hasOrg) continue;

                    // Try to find organization reference in extensions or PractitionerRole

                    if (practitioner.Extension?
                        .FirstOrDefault(e => e.Url!.Contains("service-organization"))?.Value is ResourceReference orgRef && !string.IsNullOrWhiteSpace(orgRef.Reference))
                    {
                        try
                        {
                            var org = await orgClient.ReadAsync(orgRef.Reference).ConfigureAwait(false);
                            if (org != null)
                            {
                                // Attach organization as contained resource for enrichment
                                practitioner.Contained ??= [];
                                practitioner.Contained.Add(org);
                            }
                        }
                        catch
                        {
                            // Gracefully ignore if org cannot be fetched
                        }
                    }
                }
            }

            return results;
        }


        private static string? ResolveRoleToken(PractitionerRoleShort? roleEnum, string? roleToken)
        {
            if (roleEnum.HasValue && roleEnum.Value != PractitionerRoleShort.Other)
                return RoleToToken(roleEnum.Value);

            // Fallback to caller-provided token (system/local code)
            return string.IsNullOrWhiteSpace(roleToken) ? null : roleToken;
        }

        // -----------------------------
        // Strongly-typed convenience methods
        // -----------------------------

        /// <summary>
        /// Returns Practitioners by resource ID.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="id">Logical ID of the resource.</param>
        /// <param name="includeServiceOrganization">Whether to include service organization.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Practitioner>> ByIdAsync(
            ClientConfigurator configurator,
            string id,
            bool includeServiceOrganization = false,
            int listReturnLimit = int.MaxValue,
            CancellationToken ct = default)
        {
            var p = new PractitionerSearchParams
            {
                Id = id,
                IncludeServiceOrganization = includeServiceOrganization,
                ListReturnLimit = listReturnLimit
            };
            return SearchPractitionersAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns Practitioners by name.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="name">Name to search for.</param>
        /// <param name="includeServiceOrganization">Whether to include service organization.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Practitioner>> ByNameAsync(
            ClientConfigurator configurator,
            string name,
            bool includeServiceOrganization = false,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new PractitionerSearchParams
            {
                Name = name,
                IncludeServiceOrganization = includeServiceOrganization,
                ListReturnLimit = listReturnLimit
            };
            return SearchPractitionersAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns Practitioners by active status.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="active">Active status to filter by.</param>
        /// <param name="includeServiceOrganization">Whether to include service organization.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Practitioner>> ByActiveAsync(
            ClientConfigurator configurator,
            bool active,
            bool includeServiceOrganization = false,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new PractitionerSearchParams
            {
                Active = active,
                IncludeServiceOrganization = includeServiceOrganization,
                ListReturnLimit = listReturnLimit
            };
            return SearchPractitionersAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns Practitioners by role enum.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="role">Practitioner role to filter by.</param>
        /// <param name="includeServiceOrganization">Whether to include service organization.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Practitioner>> ByRoleAsync(
            ClientConfigurator configurator,
            PractitionerRoleShort role,
            bool includeServiceOrganization = false,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new PractitionerSearchParams
            {
                PractitionerRole = role,
                IncludeServiceOrganization = includeServiceOrganization,
                ListReturnLimit = listReturnLimit
            };
            return SearchPractitionersAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns Practitioners by role token.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="roleToken">Role token to filter by.</param>
        /// <param name="includeServiceOrganization">Whether to include service organization.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Practitioner>> ByRoleTokenAsync(
            ClientConfigurator configurator,
            string roleToken,
            bool includeServiceOrganization = false,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new PractitionerSearchParams
            {
                PractitionerRoleToken = roleToken,
                IncludeServiceOrganization = includeServiceOrganization,
                ListReturnLimit = listReturnLimit
            };
            return SearchPractitionersAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns Practitioners by service organization.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="organizationRefOrToken">Organization reference or token.</param>
        /// <param name="includeServiceOrganization">Whether to include service organization.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Practitioner>> ByServiceOrganizationAsync(
            ClientConfigurator configurator,
            string organizationRefOrToken,
            bool includeServiceOrganization = true, // include by default for org queries
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new PractitionerSearchParams
            {
                ServiceOrganization = organizationRefOrToken,
                IncludeServiceOrganization = includeServiceOrganization,
                ListReturnLimit = listReturnLimit
            };
            return SearchPractitionersAsync(configurator, p, ct);
        }
    }
}
