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
    /// Provides search operations for FHIR <see cref="Encounter"/> resources using
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
    ///   <item><term><c>status</c></term><description>Encounter status (planned | arrived | triaged | in-progress | onleave | finished | cancelled | entered-in-error | unknown).</description></item>
    ///   <item><term><c>class</c></term><description>Classification of the encounter (token).</description></item>
    ///   <item><term><c>type</c></term><description>Specific type of encounter (token).</description></item>
    ///   <item><term><c>date</c></term><description>Date range boundaries (<c>ge</c>/<c>le</c> prefixes).</description></item>
    ///   <item><term><c>participant</c></term><description>Persons involved in the encounter.</description></item>
    ///   <item><term><c>location</c></term><description>Location the encounter takes place.</description></item>
    /// </list>
    /// Includes are supported via <see cref="EncounterInclude"/> and default to <see cref="EncounterInclude.Patient"/>.
    /// </remarks>
    public static class EncounterSearch
    {
        /// <summary>
        /// Encapsulates search parameters for <see cref="Encounter"/> queries.
        /// </summary>
        /// <remarks>
        /// All parameters are optional; supply any combination to constrain results.
        /// Use <see cref="ListReturnLimit"/> to defensively cap large result sets on the client side.
        /// </remarks>
        public sealed class EncounterSearchParams
        {
            /// <summary>Logical ID of the Encounter resource (FHIR <c>_id</c>).</summary>
            public string? Id { get; init; }

            /// <summary>Patient reference or id.</summary>
            public string? Patient { get; init; }

            /// <summary>One or more encounter statuses for filtering.</summary>
            public List<EncounterStatus>? Statuses { get; init; }

            /// <summary>Classification of the encounter (token, e.g., <c>AMB</c>).</summary>
            public string? Class { get; init; }

            /// <summary>Specific type of encounter (token).</summary>
            public string? Type { get; init; }

            /// <summary>Inclusive start boundary for encounter date (<c>date</c> with <c>ge</c>).</summary>
            public DateTimeOffset? DateStart { get; init; }

            /// <summary>Inclusive end boundary for encounter date (<c>date</c> with <c>le</c>).</summary>
            public DateTimeOffset? DateEnd { get; init; }

            /// <summary>Participant reference (persons involved in the encounter).</summary>
            public string? Participant { get; init; }

            /// <summary>Location reference where the encounter takes place.</summary>
            public string? Location { get; init; }

            /// <summary>
            /// FHIR _include paths for related resources.
            /// If null, defaults to including <see cref="EncounterInclude.Patient"/>.
            /// </summary>
            public IEnumerable<EncounterInclude>? Includes { get; init; }

            /// <summary>Apply <c>:iterate</c> modifier to includes if supported by the server.</summary>
            public bool UseIterateModifier { get; init; } = false;

            /// <summary>
            /// Maximum number of Encounter resources to return client-side.
            /// Defaults to <c>SearchExecutor.DefaultServerMaxResults</c> (500).
            /// </summary>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;
        }

        /// <summary>
        /// Executes a flexible Encounter search using the provided parameter bag.
        /// The <c>Statuses</c> list is fanned out into individual FHIR queries and aggregated
        /// via <see cref="FanOutSearchHelper"/>.
        /// </summary>
        /// <param name="configurator">The <see cref="ClientConfigurator"/> used to create a resource-specific client.</param>
        /// <param name="p">Search parameter bag.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A deduplicated list of <see cref="Encounter"/> resources matching the criteria.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="configurator"/> is <c>null</c>.</exception>
        public static async Task<List<Encounter>> SearchEncountersAsync(
            ClientConfigurator configurator,
            EncounterSearchParams p,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new EncounterSearchParams();

            var limit = SearchExecutor.NormalizeLimit(p.ListReturnLimit);
            var modifier = p.UseIterateModifier ? IncludeModifier.Iterate : IncludeModifier.None;

            Builder<Encounter> MakeBaseBuilder()
            {
                var builder = new Builder<Encounter>();

                if (!string.IsNullOrWhiteSpace(p.Id))
                    builder.With("_id", p.Id);
                if (!string.IsNullOrWhiteSpace(p.Patient))
                    builder.With("patient", p.Patient);
                if (!string.IsNullOrWhiteSpace(p.Class))
                    builder.With("class", p.Class);
                if (!string.IsNullOrWhiteSpace(p.Type))
                    builder.With("type", p.Type);
                if (!string.IsNullOrWhiteSpace(p.Participant))
                    builder.With("participant", p.Participant);
                if (!string.IsNullOrWhiteSpace(p.Location))
                    builder.With("location", p.Location);

                if (p.DateStart.HasValue)
                    builder.With("date", $"ge{p.DateStart.Value:O}");
                if (p.DateEnd.HasValue)
                    builder.With("date", $"le{p.DateEnd.Value:O}");

                if (p.Includes is not null && p.Includes.Any())
                    builder.Include(p.Includes, modifier: modifier);
                else
                    builder.Include(EncounterInclude.Patient);

                if (limit != int.MaxValue)
                    builder.WithCount(limit);
                return builder;
            }

            var fanOuts = new List<FanOutSearchHelper.FanOutParam>();
            if (p.Statuses is { Count: > 0 })
                fanOuts.Add(new FanOutSearchHelper.FanOutParam("status",
                    p.Statuses.Select(s => EncounterStatusToToken(s)).ToList()));

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
        /// Returns encounters for a specific patient.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="patient">Patient reference or id.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Encounter>> ByPatientAsync(
            ClientConfigurator configurator,
            string patient,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<EncounterInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new EncounterSearchParams
            {
                Patient = patient,
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchEncountersAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns a single encounter by its logical id.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="id">Logical id of the resource.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Encounter>> ByIdAsync(
            ClientConfigurator configurator,
            string id,
            IEnumerable<EncounterInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new EncounterSearchParams
            {
                Id = id,
                ListReturnLimit = 1,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchEncountersAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns encounters filtered by status.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="statuses">Encounter statuses to filter by.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Encounter>> ByStatusAsync(
            ClientConfigurator configurator,
            IEnumerable<EncounterStatus> statuses,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<EncounterInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new EncounterSearchParams
            {
                Statuses = statuses?.ToList() ?? new(),
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchEncountersAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns encounters within a date window.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="startInclusive">Inclusive start of the date window.</param>
        /// <param name="endInclusive">Inclusive end of the date window.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Encounter>> ByDateWindowAsync(
            ClientConfigurator configurator,
            DateTimeOffset startInclusive,
            DateTimeOffset endInclusive,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<EncounterInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new EncounterSearchParams
            {
                DateStart = startInclusive,
                DateEnd = endInclusive,
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchEncountersAsync(configurator, p, ct);
        }
    }
}
