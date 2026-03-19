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
    /// Provides search operations for FHIR <see cref="Condition"/> resources using
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
    ///   <item><term><c>is-external</c></term><description>Custom boolean flag indicating an external source (true | false).</description></item>
    ///   <item><term><c>category</c></term><description>Token (e.g., <c>encounter-diagnosis</c> or <c>75326-9</c>).</description></item>
    ///   <item><term><c>clinical-status</c></term><description>Status (active | inactive | resolved | subsided | controlled | progressed | other | ruled-out | cured | in-remission).</description></item>
    ///   <item><term><c>onset-date</c></term><description>Date or period boundaries for onset (supports prefixes <c>ge</c> / <c>le</c> for range).</description></item>
    ///   <item><term><c>patient</c></term><description>Patient reference.</description></item>
    ///   <item><term><c>rank</c></term><description>Numeric ranking (1–9).</description></item>
    ///   <item><term><c>subject</c></term><description>Subject reference (often Patient or Group).</description></item>
    ///   <item><term><c>verification-status</c></term><description>Verification status (provisional | confirmed | entered-in-error).</description></item>
    /// </list>
    /// Includes are supported via <see cref="ConditionInclude"/> and default to <see cref="ConditionInclude.Patient"/>.
    /// </remarks>
    public static class ConditionSearch
    {
        /// <summary>
        /// Encapsulates search parameters for <see cref="Condition"/> queries.
        /// </summary>
        /// <remarks>
        /// All parameters are optional; supply any combination to constrain results.
        /// Use <see cref="ListReturnLimit"/> to defensively cap large result sets on the client side.
        /// </remarks>
        public sealed class ConditionSearchParams
        {
            /// <summary>
            /// Logical ID of the Condition resource (FHIR <c>_id</c>).
            /// </summary>
            public string? Id { get; init; }

            /// <summary>
            /// Flags whether the condition originates externally (custom boolean search parameter).
            /// </summary>
            /// <remarks>
            /// This maps to the search parameter key <c>IsExternal</c> (PascalCase, as expected by the
            /// Aria FHIR server).
            /// </remarks>
            public bool? IsExternal { get; init; }

            /// <summary>
            /// One or more category tokens (e.g., <c>encounter-diagnosis</c> or <c>75326-9</c>).
            /// </summary>
            /// <remarks>
            /// Accepts code-only or <c>system|code</c> forms per FHIR token semantics.
            /// </remarks>
            public List<string>? Categories { get; init; }

            /// <summary>
            /// One or more clinical statuses for filtering.
            /// </summary>
            public List<ClinicalStatus>? ClinicalStatuses { get; init; }

            /// <summary>
            /// Inclusive start boundary for onset date/time (<c>onset-date</c> with <c>ge</c>).
            /// </summary>
            public DateTimeOffset? OnsetStart { get; init; }

            /// <summary>
            /// Inclusive end boundary for onset date/time (<c>onset-date</c> with <c>le</c>).
            /// </summary>
            public DateTimeOffset? OnsetEnd { get; init; }

            /// <summary>
            /// Patient reference (id or reference string).
            /// </summary>
            public string? Patient { get; init; }

            /// <summary>
            /// Optional numeric rank (1–9).
            /// </summary>
            public int? Rank { get; init; }

            /// <summary>
            /// Subject reference (often Patient or Group).
            /// </summary>
            public string? Subject { get; init; }

            /// <summary>
            /// One or more verification statuses for filtering.
            /// </summary>
            public List<VerificationState>? VerificationStatuses { get; init; }

            /// <summary>
            /// FHIR _include paths for related resources.
            /// If null, defaults to including <see cref="ConditionInclude.Patient"/>.
            /// </summary>
            public IEnumerable<ConditionInclude>? Includes { get; init; }

            /// <summary>
            /// Apply <c>:iterate</c> modifier to includes if supported by the server.
            /// </summary>
            public bool UseIterateModifier { get; init; } = false;

            /// <summary>
            /// Maximum number of Condition resources to return client-side.
            /// Defaults to <c>SearchExecutor.DefaultServerMaxResults</c> (500).
            /// </summary>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;
        }

        /// <summary>
        /// Executes a flexible Condition search using the provided parameter bag.
        /// List parameters (Categories, ClinicalStatuses, VerificationStatuses) are fanned out
        /// into individual FHIR queries and aggregated with OR/AND semantics via
        /// <see cref="FanOutSearchHelper"/>.
        /// </summary>
        /// <param name="configurator">The <see cref="ClientConfigurator"/> used to create a resource-specific client.</param>
        /// <param name="p">Search parameter bag containing id, is-external, category, clinical-status, onset window, patient, rank, subject, verification-status, includes, and limits.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A deduplicated list of <see cref="Condition"/> resources matching the criteria.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="configurator"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <c>Rank</c> is provided but outside 1–9.</exception>
        public static async Task<List<Condition>> SearchConditionsAsync(
            ClientConfigurator configurator,
            ConditionSearchParams p,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new ConditionSearchParams();

            if (p.Rank.HasValue && (p.Rank < 1 || p.Rank > 9))
                throw new ArgumentOutOfRangeException(nameof(p.Rank), "Rank must be a number from 1 to 9.");

            var limit = SearchExecutor.NormalizeLimit(p.ListReturnLimit);

            Builder<Condition> MakeBaseBuilder()
            {
                var builder = new Builder<Condition>();

                if (!string.IsNullOrWhiteSpace(p.Id))
                    builder.With("_id", p.Id);
                if (p.IsExternal.HasValue)
                    builder.With("IsExternal", p.IsExternal.Value ? "true" : "false");

                if (p.OnsetStart.HasValue)
                    builder.With("onset-date", $"ge{p.OnsetStart.Value:O}");
                if (p.OnsetEnd.HasValue)
                    builder.With("onset-date", $"le{p.OnsetEnd.Value:O}");

                if (!string.IsNullOrWhiteSpace(p.Patient))
                    builder.With("patient", p.Patient);
                if (p.Rank.HasValue)
                    builder.With("rank", p.Rank.Value.ToString());
                if (!string.IsNullOrWhiteSpace(p.Subject))
                    builder.With("subject", p.Subject);

                var modifier = IncludeModifier.None;
                if (p.Includes is not null && p.Includes.Any())
                    builder.Include(p.Includes, modifier: modifier);
                else
                    builder.Include(ConditionInclude.Patient);

                if (limit != int.MaxValue)
                    builder.WithCount(limit);
                return builder;
            }

            var fanOuts = new List<FanOutSearchHelper.FanOutParam>();
            if (p.Categories is { Count: > 0 })
            {
                var values = p.Categories.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                if (values.Count > 0)
                    fanOuts.Add(new FanOutSearchHelper.FanOutParam("category", values));
            }
            if (p.ClinicalStatuses is { Count: > 0 })
                fanOuts.Add(new FanOutSearchHelper.FanOutParam("clinical-status",
                    p.ClinicalStatuses.Select(cs => ClinicalStatusToToken(cs)).ToList()));
            if (p.VerificationStatuses is { Count: > 0 })
                fanOuts.Add(new FanOutSearchHelper.FanOutParam("verification-status",
                    p.VerificationStatuses.Select(vs => VerificationStatusToToken(vs)).ToList()));

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
        /// Returns conditions for a specific patient.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="patient">Patient reference or id.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Condition>> ByPatientAsync(
            ClientConfigurator configurator,
            string patient,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<ConditionInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new ConditionSearchParams
            {
                Patient = patient,
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchConditionsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns conditions filtered by clinical status.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="statuses">Clinical statuses to filter by.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Condition>> ByClinicalStatusAsync(
            ClientConfigurator configurator,
            IEnumerable<ClinicalStatus> statuses,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<ConditionInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new ConditionSearchParams
            {
                ClinicalStatuses = statuses?.ToList() ?? new(),
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchConditionsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns conditions filtered by verification status.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="statuses">Verification statuses to filter by.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Condition>> ByVerificationStatusAsync(
            ClientConfigurator configurator,
            IEnumerable<VerificationState> statuses,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<ConditionInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new ConditionSearchParams
            {
                VerificationStatuses = statuses?.ToList() ?? new(),
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchConditionsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns conditions by category token(s) (e.g., 'encounter-diagnosis', '75326-9').
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="categories">Category tokens to filter by.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Condition>> ByCategoriesAsync(
            ClientConfigurator configurator,
            IEnumerable<string> categories,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<ConditionInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new ConditionSearchParams
            {
                Categories = categories?.Where(s => !string.IsNullOrWhiteSpace(s)).ToList() ?? new(),
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchConditionsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns conditions within an onset date/time window.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="startInclusive">Inclusive start of onset window.</param>
        /// <param name="endInclusive">Inclusive end of onset window.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Condition>> ByOnsetWindowAsync(
            ClientConfigurator configurator,
            DateTimeOffset startInclusive,
            DateTimeOffset endInclusive,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<ConditionInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new ConditionSearchParams
            {
                OnsetStart = startInclusive,
                OnsetEnd = endInclusive,
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchConditionsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns a single condition by its logical id.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="id">Logical id of the resource.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Condition>> ByIdAsync(
            ClientConfigurator configurator,
            string id,
            IEnumerable<ConditionInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new ConditionSearchParams
            {
                Id = id,
                ListReturnLimit = 1,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchConditionsAsync(configurator, p, ct);
        }


    }
}
