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
    /// Provides search operations for FHIR <see cref="ChargeItem"/> resources using
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
    ///   <item><term><c>category</c></term><description>Charge category (Technical | Administrative | Global | Medication Oncology | Professional).</description></item>
    ///   <item><term><c>completedOn</c></term><description>Date/time when the charge was completed.</description></item>
    ///   <item><term><c>lastUpdated</c></term><description>Last updated timestamp for the resource.</description></item>
    ///   <item><term><c>occurrence</c></term><description>Date/time when the charge service was applied.</description></item>
    ///   <item><term><c>patient</c></term><description>Patient reference associated with the charge.</description></item>
    ///   <item><term><c>performing-organization</c></term><description>Organization performing the charge service.</description></item>
    ///   <item><term><c>status</c></term><description>Status of the charge (planned | billable | not-billable | billed | entered-in-error).</description></item>
    ///   <item><term><c>subject</c></term><description>The subject of the charge (often a Patient or Group).</description></item>
    /// </list>
    /// Includes supported via <see cref="ChargeItemInclude"/> enum.
    /// </remarks>
    public static class ChargeItemSearch
    {

        /// <summary>
        /// Encapsulates search parameters for ChargeItem queries.
        /// </summary>
        public sealed class ChargeItemSearchParams
        {
            /// <summary>Logical ID of the ChargeItem resource.</summary>
            public string? Id { get; init; }

            /// <summary>One or more charge categories to filter by.</summary>
            public List<ChargeCategory>? Categories { get; init; }

            /// <summary>Date/time when the charge was completed.</summary>
            public DateTimeOffset? CompletedOn { get; init; }

            /// <summary>Last updated timestamp for the resource.</summary>
            public DateTimeOffset? LastUpdated { get; init; }

            /// <summary>Date/time when the charge service was applied.</summary>
            public DateTimeOffset? Occurrence { get; init; }

            /// <summary>Patient reference associated with the charge.</summary>
            public string? Patient { get; init; }

            /// <summary>Organization performing the charge service.</summary>
            public string? PerformingOrganization { get; init; }

            /// <summary>One or more statuses to filter by.</summary>
            public List<ChargeStatus>? Statuses { get; init; }

            /// <summary>The subject of the charge (often a Patient or Group).</summary>
            public string? Subject { get; init; }

            /// <summary>FHIR _include paths for related resources.</summary>
            public IEnumerable<ChargeItemInclude>? Includes { get; init; }

            /// <summary>Apply :iterate modifier to includes if supported by the server.</summary>
            public bool UseIterateModifier { get; init; } = false;

            /// <summary>Maximum number of ChargeItem resources to return. Defaults to <c>SearchExecutor.DefaultServerMaxResults</c> (500).</summary>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;
        }

        /// <summary>
        /// Executes a flexible ChargeItem search using the provided parameter bag.
        /// List parameters (Categories, Statuses) are fanned out into individual FHIR queries
        /// and aggregated with OR/AND semantics via <see cref="FanOutSearchHelper"/>.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="p">Search parameter bag.</param>
        /// <param name="ct">Cancellation token.</param>
        public static async Task<List<ChargeItem>> SearchChargeItemsAsync(
            ClientConfigurator configurator,
            ChargeItemSearchParams p,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new ChargeItemSearchParams();

            var limit = SearchExecutor.NormalizeLimit(p.ListReturnLimit);

            Builder<ChargeItem> MakeBaseBuilder()
            {
                var builder = new Builder<ChargeItem>();

                if (!string.IsNullOrWhiteSpace(p.Id))
                    builder.With("_id", p.Id);
                if (p.CompletedOn.HasValue)
                    builder.With("completedOn", p.CompletedOn.Value.ToString("O"));
                if (p.LastUpdated.HasValue)
                    builder.With("_lastUpdated", p.LastUpdated.Value.ToString("O"));
                if (p.Occurrence.HasValue)
                    builder.With("occurrence", p.Occurrence.Value.ToString("O"));
                if (!string.IsNullOrWhiteSpace(p.Patient))
                    builder.With("patient", p.Patient);
                if (!string.IsNullOrWhiteSpace(p.PerformingOrganization))
                    builder.With("performing-organization", p.PerformingOrganization);
                if (!string.IsNullOrWhiteSpace(p.Subject))
                    builder.With("subject", p.Subject);

                var modifier = IncludeModifier.None;
                if (p.Includes is not null && p.Includes.Any())
                    builder.Include(p.Includes, modifier: modifier);
                else
                    builder.Include(ChargeItemInclude.Patient);

                if (limit != int.MaxValue)
                    builder.WithCount(limit);
                return builder;
            }

            var fanOuts = new List<FanOutSearchHelper.FanOutParam>();
            if (p.Categories is { Count: > 0 })
                fanOuts.Add(new FanOutSearchHelper.FanOutParam("category",
                    p.Categories.Select(cat => CategoryToParam(cat)).ToList()));
            if (p.Statuses is { Count: > 0 })
                fanOuts.Add(new FanOutSearchHelper.FanOutParam("status",
                    p.Statuses.Select(s => StatusToParam(s)).ToList()));

            return await SearchExecutor.ExecuteAsync(
                configurator,
                MakeBaseBuilder,
                fanOuts,
                limit,
                ct).ConfigureAwait(false);
        }

        // -----------------------------
        // Convenience Methods
        // -----------------------------

        /// <summary>
        /// Returns charge items matching the specified logical id.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="id">Logical ID of the resource.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<ChargeItem>> ByIdAsync(ClientConfigurator configurator, string id, int listReturnLimit = int.MaxValue, CancellationToken ct = default)
        {
            var p = new ChargeItemSearchParams { Id = id, ListReturnLimit = listReturnLimit };
            return SearchChargeItemsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns charge items for a specific patient reference.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="patient">Patient reference or id.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<ChargeItem>> ByPatientAsync(ClientConfigurator configurator, string patient, int listReturnLimit = SearchExecutor.DefaultServerMaxResults, CancellationToken ct = default)
        {
            var p = new ChargeItemSearchParams { Patient = patient, ListReturnLimit = listReturnLimit };
            return SearchChargeItemsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns charge items matching the specified category.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="category">Charge category to filter by.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<ChargeItem>> ByCategoryAsync(ClientConfigurator configurator, ChargeCategory category, int listReturnLimit = SearchExecutor.DefaultServerMaxResults, CancellationToken ct = default)
        {
            var p = new ChargeItemSearchParams { Categories = new List<ChargeCategory> { category }, ListReturnLimit = listReturnLimit };
            return SearchChargeItemsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns charge items matching the specified status.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="status">Charge status to filter by.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<ChargeItem>> ByStatusAsync(ClientConfigurator configurator, ChargeStatus status, int listReturnLimit = SearchExecutor.DefaultServerMaxResults, CancellationToken ct = default)
        {
            var p = new ChargeItemSearchParams { Statuses = new List<ChargeStatus> { status }, ListReturnLimit = listReturnLimit };
            return SearchChargeItemsAsync(configurator, p, ct);
        }

    }
}
