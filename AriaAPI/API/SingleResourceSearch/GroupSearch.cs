// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.API.SearchHelpers;
using AriaAPI.Core;
using AriaAPI.Networking.Core;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static AriaAPI.API.SearchHelpers.SearchHelpers;
using AriaAPI.Resources.Includes;
using static AriaAPI.API.SearchHelpers.SearchTypes;

namespace AriaAPI.API.SingleResourceSearch
{
    /// <summary>
    /// Provides search operations for FHIR <see cref="Group"/> resources using
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
    ///   <item><term><c>active</c></term><description>Boolean flag indicating if the group is active (true | false).</description></item>
    ///   <item><term><c>code</c></term><description>Classification of the group (Staff | Resource | ResourceAndStaff).</description></item>
    ///   <item><term><c>managing-entity</c></term><description>Entity that manages the group definition.</description></item>
    ///   <item><term><c>member</c></term><description>Reference to a group member.</description></item>
    ///   <item><term><c>name</c></term><description>Label for the group.</description></item>
    /// </list>
    /// Includes:
    /// <list type="bullet">
    ///   <item><description><c>Group:member</c> (via <see cref="GroupInclude.Member"/>)</description></item>
    /// </list>
    /// </remarks>
    public static class GroupSearch
    {
        /// <summary>
        /// Encapsulates search parameters for Group queries.
        /// </summary>
        public sealed class GroupSearchParams
        {
            /// <summary>Logical ID of the Group resource.</summary>
            public string? Id { get; init; }

            /// <summary>Boolean flag indicating if the group is active.</summary>
            public bool? Active { get; init; }

            /// <summary>
            /// Classification of the group (FHIR search parameter <c>code</c>).
            /// </summary>
            public GroupCode? Code { get; init; }

            /// <summary>Entity that manages the group definition.</summary>
            public string? ManagingEntity { get; init; }

            /// <summary>Reference to a group member.</summary>
            public string? Member { get; init; }

            /// <summary>Label for the group.</summary>
            public string? Name { get; init; }

            /// <summary>
            /// Whether to include related Group members in the search results.
            /// </summary>
            public bool IncludeMembers { get; init; } = false;

            /// <summary>Maximum number of results to return. Defaults to <c>SearchExecutor.DefaultServerMaxResults</c> (500).</summary>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;
        }

        /// <summary>
        /// Executes a Group search using the provided parameter bag.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="p">Search parameter bag.</param>
        /// <param name="ct">Cancellation token.</param>
        public static async Task<List<Group>> SearchGroupsAsync(
            ClientConfigurator configurator,
            GroupSearchParams p,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new GroupSearchParams();

            var limit = SearchExecutor.NormalizeLimit(p.ListReturnLimit);

            return await SearchExecutor.ExecuteAsync(
                configurator,
                () =>
                {
                    var builder = new Builder<Group>();

                    if (!string.IsNullOrWhiteSpace(p.Id))
                        builder.With("_id", p.Id);

                    if (p.Active.HasValue)
                        builder.With("active", p.Active.Value ? "true" : "false");

                    if (p.Code.HasValue)
                        builder.With("code", GroupCodeToToken(p.Code.Value));

                    if (!string.IsNullOrWhiteSpace(p.ManagingEntity))
                        builder.With("managing-entity", p.ManagingEntity);

                    if (!string.IsNullOrWhiteSpace(p.Member))
                        builder.With("member", p.Member);

                    if (!string.IsNullOrWhiteSpace(p.Name))
                        builder.With("name", p.Name);

                    if (p.IncludeMembers)
                    {
                        builder.Include(FhirIncludeResources.GetResourceNameFor<GroupInclude>(), GroupInclude.Member.ToSegment());
                    }

                    if (limit != int.MaxValue)
                        builder.WithCount(limit);
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
        /// Returns Groups by resource ID.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="id">Logical ID of the resource.</param>
        /// <param name="includeMembers">Whether to include group members.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Group>> ByIdAsync(ClientConfigurator configurator, string id, bool includeMembers = false, int listReturnLimit = int.MaxValue, CancellationToken ct = default)
        {
            var p = new GroupSearchParams { Id = id, IncludeMembers = includeMembers, ListReturnLimit = listReturnLimit };
            return SearchGroupsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns Groups by name.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="name">Name to search for.</param>
        /// <param name="includeMembers">Whether to include group members.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Group>> ByNameAsync(ClientConfigurator configurator, string name, bool includeMembers = false, int listReturnLimit = SearchExecutor.DefaultServerMaxResults, CancellationToken ct = default)
        {
            var p = new GroupSearchParams { Name = name, IncludeMembers = includeMembers, ListReturnLimit = listReturnLimit };
            return SearchGroupsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns Groups by active status.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="active">Active status to filter by.</param>
        /// <param name="includeMembers">Whether to include group members.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Group>> ByActiveAsync(ClientConfigurator configurator, bool active, bool includeMembers = false, int listReturnLimit = SearchExecutor.DefaultServerMaxResults, CancellationToken ct = default)
        {
            var p = new GroupSearchParams { Active = active, IncludeMembers = includeMembers, ListReturnLimit = listReturnLimit };
            return SearchGroupsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns Groups by code.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="code">Group code to filter by.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Group>> ByCodeAsync(ClientConfigurator configurator, GroupCode code, int listReturnLimit = SearchExecutor.DefaultServerMaxResults, CancellationToken ct = default)
        {
            var p = new GroupSearchParams { Code = code, ListReturnLimit = listReturnLimit };
            return SearchGroupsAsync(configurator, p, ct);
        }

    }
}
