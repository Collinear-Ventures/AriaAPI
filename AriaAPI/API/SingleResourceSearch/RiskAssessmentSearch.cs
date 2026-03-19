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
    /// Provides search operations for FHIR <see cref="RiskAssessment"/> resources using
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
    ///   <item><term><c>subject</c></term><description>Who/what does assessment apply to.</description></item>
    ///   <item><term><c>method</c></term><description>Evaluation mechanism (token).</description></item>
    ///   <item><term><c>date</c></term><description>When was assessment made (<c>ge</c>/<c>le</c> prefixes).</description></item>
    ///   <item><term><c>condition</c></term><description>Condition assessed.</description></item>
    /// </list>
    /// Includes are supported via <see cref="RiskAssessmentInclude"/> and default to <see cref="RiskAssessmentInclude.Patient"/>.
    /// </remarks>
    public static class RiskAssessmentSearch
    {
        /// <summary>
        /// Encapsulates search parameters for <see cref="RiskAssessment"/> queries.
        /// </summary>
        public sealed class RiskAssessmentSearchParams
        {
            /// <summary>Logical ID of the RiskAssessment resource (FHIR <c>_id</c>).</summary>
            public string? Id { get; init; }

            /// <summary>Patient reference or id.</summary>
            public string? Patient { get; init; }

            /// <summary>Who/what does assessment apply to (reference or id).</summary>
            public string? Subject { get; init; }

            /// <summary>Evaluation mechanism token.</summary>
            public string? Method { get; init; }

            /// <summary>Inclusive start boundary for assessment date (<c>date</c> with <c>ge</c>).</summary>
            public DateTimeOffset? DateStart { get; init; }

            /// <summary>Inclusive end boundary for assessment date (<c>date</c> with <c>le</c>).</summary>
            public DateTimeOffset? DateEnd { get; init; }

            /// <summary>Condition assessed (reference or id).</summary>
            public string? Condition { get; init; }

            /// <summary>
            /// FHIR _include paths for related resources.
            /// If null, defaults to including <see cref="RiskAssessmentInclude.Patient"/>.
            /// </summary>
            public IEnumerable<RiskAssessmentInclude>? Includes { get; init; }

            /// <summary>Apply <c>:iterate</c> modifier to includes if supported by the server.</summary>
            public bool UseIterateModifier { get; init; } = false;

            /// <summary>
            /// Maximum number of RiskAssessment resources to return client-side.
            /// Defaults to <c>SearchExecutor.DefaultServerMaxResults</c> (500).
            /// </summary>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;
        }

        /// <summary>
        /// Executes a flexible RiskAssessment search using the provided parameter bag.
        /// Builds FHIR <see cref="SearchParams"/> via <c>Builder&lt;T&gt;</c> and aggregates results across pages.
        /// </summary>
        /// <param name="configurator">The <see cref="ClientConfigurator"/> used to create a resource-specific client.</param>
        /// <param name="p">Search parameter bag.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A deduplicated list of <see cref="RiskAssessment"/> resources matching the criteria.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="configurator"/> is <c>null</c>.</exception>
        public static async Task<List<RiskAssessment>> SearchRiskAssessmentsAsync(
            ClientConfigurator configurator,
            RiskAssessmentSearchParams p,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new RiskAssessmentSearchParams();

            var limit = SearchExecutor.NormalizeLimit(p.ListReturnLimit);
            var modifier = p.UseIterateModifier ? IncludeModifier.Iterate : IncludeModifier.None;

            Builder<RiskAssessment> MakeBaseBuilder()
            {
                var builder = new Builder<RiskAssessment>();

                if (!string.IsNullOrWhiteSpace(p.Id))
                    builder.With("_id", p.Id);
                if (!string.IsNullOrWhiteSpace(p.Patient))
                    builder.With("patient", p.Patient);
                if (!string.IsNullOrWhiteSpace(p.Subject))
                    builder.With("subject", p.Subject);
                if (!string.IsNullOrWhiteSpace(p.Method))
                    builder.With("method", p.Method);
                if (!string.IsNullOrWhiteSpace(p.Condition))
                    builder.With("condition", p.Condition);

                if (p.DateStart.HasValue)
                    builder.With("date", $"ge{p.DateStart.Value:O}");
                if (p.DateEnd.HasValue)
                    builder.With("date", $"le{p.DateEnd.Value:O}");

                if (p.Includes is not null && p.Includes.Any())
                    builder.Include(p.Includes, modifier: modifier);
                else
                    builder.Include(RiskAssessmentInclude.Patient);

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
        /// Returns risk assessments for a specific patient.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="patient">Patient reference or id.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<RiskAssessment>> ByPatientAsync(
            ClientConfigurator configurator,
            string patient,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<RiskAssessmentInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new RiskAssessmentSearchParams
            {
                Patient = patient,
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchRiskAssessmentsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns a single risk assessment by its logical id.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="id">Logical id of the resource.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<RiskAssessment>> ByIdAsync(
            ClientConfigurator configurator,
            string id,
            IEnumerable<RiskAssessmentInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new RiskAssessmentSearchParams
            {
                Id = id,
                ListReturnLimit = 1,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchRiskAssessmentsAsync(configurator, p, ct);
        }
    }
}
