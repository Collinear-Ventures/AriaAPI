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
    /// Provides search operations for FHIR <see cref="MedicationRequest"/> resources using
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
    ///   <item><term><c>patient</c></term><description>Patient reference.</description></item>
    ///   <item><term><c>status</c></term><description>Request status (active | on-hold | cancelled | completed | entered-in-error | stopped | draft | unknown).</description></item>
    ///   <item><term><c>intent</c></term><description>Type of medication usage (token).</description></item>
    ///   <item><term><c>medication</c></term><description>Medication reference or code.</description></item>
    ///   <item><term><c>authoredon</c></term><description>Authored-on date range (<c>ge</c>/<c>le</c> prefixes).</description></item>
    ///   <item><term><c>requester</c></term><description>Who/what requested the medication.</description></item>
    /// </list>
    /// Includes are supported via <see cref="MedicationRequestInclude"/> and default to <see cref="MedicationRequestInclude.Patient"/>.
    /// </remarks>
    public static class MedicationRequestSearch
    {
        /// <summary>
        /// Encapsulates search parameters for <see cref="MedicationRequest"/> queries.
        /// </summary>
        public sealed class MedicationRequestSearchParams
        {
            /// <summary>Logical ID of the MedicationRequest resource (FHIR <c>_id</c>).</summary>
            public string? Id { get; init; }

            /// <summary>Patient reference or id.</summary>
            public string? Patient { get; init; }

            /// <summary>One or more request statuses for filtering.</summary>
            public List<MedicationRequestStatus>? Statuses { get; init; }

            /// <summary>Type of medication usage (token, e.g., <c>order</c>).</summary>
            public string? Intent { get; init; }

            /// <summary>Medication reference or code (token).</summary>
            public string? Medication { get; init; }

            /// <summary>Inclusive start boundary for authored-on date (<c>authoredon</c> with <c>ge</c>).</summary>
            public DateTimeOffset? AuthoredOnStart { get; init; }

            /// <summary>Inclusive end boundary for authored-on date (<c>authoredon</c> with <c>le</c>).</summary>
            public DateTimeOffset? AuthoredOnEnd { get; init; }

            /// <summary>Who/what requested the medication (reference or id).</summary>
            public string? Requester { get; init; }

            /// <summary>
            /// FHIR _include paths for related resources.
            /// If null, defaults to including <see cref="MedicationRequestInclude.Patient"/>.
            /// </summary>
            public IEnumerable<MedicationRequestInclude>? Includes { get; init; }

            /// <summary>Apply <c>:iterate</c> modifier to includes if supported by the server.</summary>
            public bool UseIterateModifier { get; init; } = false;

            /// <summary>
            /// Maximum number of MedicationRequest resources to return client-side.
            /// Defaults to <c>SearchExecutor.DefaultServerMaxResults</c> (500).
            /// </summary>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;
        }

        /// <summary>
        /// Executes a flexible MedicationRequest search using the provided parameter bag.
        /// The <c>Statuses</c> list is fanned out into individual FHIR queries and aggregated
        /// via <see cref="FanOutSearchHelper"/>.
        /// </summary>
        /// <param name="configurator">The <see cref="ClientConfigurator"/> used to create a resource-specific client.</param>
        /// <param name="p">Search parameter bag.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A deduplicated list of <see cref="MedicationRequest"/> resources matching the criteria.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="configurator"/> is <c>null</c>.</exception>
        public static async Task<List<MedicationRequest>> SearchMedicationRequestsAsync(
            ClientConfigurator configurator,
            MedicationRequestSearchParams p,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new MedicationRequestSearchParams();

            var limit = SearchExecutor.NormalizeLimit(p.ListReturnLimit);
            var modifier = p.UseIterateModifier ? IncludeModifier.Iterate : IncludeModifier.None;

            Builder<MedicationRequest> MakeBaseBuilder()
            {
                var builder = new Builder<MedicationRequest>();

                if (!string.IsNullOrWhiteSpace(p.Id))
                    builder.With("_id", p.Id);
                if (!string.IsNullOrWhiteSpace(p.Patient))
                    builder.With("patient", p.Patient);
                if (!string.IsNullOrWhiteSpace(p.Intent))
                    builder.With("intent", p.Intent);
                if (!string.IsNullOrWhiteSpace(p.Medication))
                    builder.With("medication", p.Medication);
                if (!string.IsNullOrWhiteSpace(p.Requester))
                    builder.With("requester", p.Requester);

                if (p.AuthoredOnStart.HasValue)
                    builder.With("authoredon", $"ge{p.AuthoredOnStart.Value:O}");
                if (p.AuthoredOnEnd.HasValue)
                    builder.With("authoredon", $"le{p.AuthoredOnEnd.Value:O}");

                if (p.Includes is not null && p.Includes.Any())
                    builder.Include(p.Includes, modifier: modifier);
                else
                    builder.Include(MedicationRequestInclude.Patient);

                if (limit != int.MaxValue)
                    builder.WithCount(limit);
                return builder;
            }

            var fanOuts = new List<FanOutSearchHelper.FanOutParam>();
            if (p.Statuses is { Count: > 0 })
                fanOuts.Add(new FanOutSearchHelper.FanOutParam("status",
                    p.Statuses.Select(s => MedicationRequestStatusToToken(s)).ToList()));

            return await SearchExecutor.ExecuteAsync(
                configurator,
                MakeBaseBuilder,
                fanOuts,
                limit,
                ct).ConfigureAwait(false);
        }

        // -----------------------------
        // Strongly-typed convenience methods
        // -----------------------------

        /// <summary>
        /// Returns medication requests for a specific patient.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="patient">Patient reference or id.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<MedicationRequest>> ByPatientAsync(
            ClientConfigurator configurator,
            string patient,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<MedicationRequestInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new MedicationRequestSearchParams
            {
                Patient = patient,
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchMedicationRequestsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns a single medication request by its logical id.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="id">Logical id of the resource.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<MedicationRequest>> ByIdAsync(
            ClientConfigurator configurator,
            string id,
            IEnumerable<MedicationRequestInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new MedicationRequestSearchParams
            {
                Id = id,
                ListReturnLimit = 1,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchMedicationRequestsAsync(configurator, p, ct);
        }
    }
}
