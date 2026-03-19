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
    /// Provides search operations for FHIR <see cref="DiagnosticReport"/> resources using
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
    ///   <item><term><c>status</c></term><description>Report status (registered | partial | preliminary | final | amended | corrected | appended | cancelled | entered-in-error | unknown).</description></item>
    ///   <item><term><c>category</c></term><description>Report category code (token).</description></item>
    ///   <item><term><c>code</c></term><description>Report type code (token).</description></item>
    ///   <item><term><c>issued</c></term><description>Date/time issued (<c>ge</c>/<c>le</c> prefixes).</description></item>
    ///   <item><term><c>performer</c></term><description>Who is responsible for the report.</description></item>
    /// </list>
    /// Includes are supported via <see cref="DiagnosticReportInclude"/> and default to <see cref="DiagnosticReportInclude.Patient"/>.
    /// </remarks>
    public static class DiagnosticReportSearch
    {
        /// <summary>
        /// Encapsulates search parameters for <see cref="DiagnosticReport"/> queries.
        /// </summary>
        public sealed class DiagnosticReportSearchParams
        {
            /// <summary>Logical ID of the DiagnosticReport resource (FHIR <c>_id</c>).</summary>
            public string? Id { get; init; }

            /// <summary>Patient reference or id.</summary>
            public string? Patient { get; init; }

            /// <summary>One or more report statuses for filtering.</summary>
            public List<DiagnosticReportStatus>? Statuses { get; init; }

            /// <summary>Report category code (token).</summary>
            public string? Category { get; init; }

            /// <summary>Report type code (token).</summary>
            public string? Code { get; init; }

            /// <summary>Inclusive start boundary for issued date (<c>issued</c> with <c>ge</c>).</summary>
            public DateTimeOffset? IssuedStart { get; init; }

            /// <summary>Inclusive end boundary for issued date (<c>issued</c> with <c>le</c>).</summary>
            public DateTimeOffset? IssuedEnd { get; init; }

            /// <summary>Who is responsible for the report (reference or id).</summary>
            public string? Performer { get; init; }

            /// <summary>
            /// FHIR _include paths for related resources.
            /// If null, defaults to including <see cref="DiagnosticReportInclude.Patient"/>.
            /// </summary>
            public IEnumerable<DiagnosticReportInclude>? Includes { get; init; }

            /// <summary>Apply <c>:iterate</c> modifier to includes if supported by the server.</summary>
            public bool UseIterateModifier { get; init; } = false;

            /// <summary>
            /// Maximum number of DiagnosticReport resources to return client-side.
            /// Defaults to <c>SearchExecutor.DefaultServerMaxResults</c> (500).
            /// </summary>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;
        }

        /// <summary>
        /// Executes a flexible DiagnosticReport search using the provided parameter bag.
        /// The <c>Statuses</c> list is fanned out into individual FHIR queries and aggregated
        /// via <see cref="FanOutSearchHelper"/>.
        /// </summary>
        /// <param name="configurator">The <see cref="ClientConfigurator"/> used to create a resource-specific client.</param>
        /// <param name="p">Search parameter bag.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A deduplicated list of <see cref="DiagnosticReport"/> resources matching the criteria.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="configurator"/> is <c>null</c>.</exception>
        public static async Task<List<DiagnosticReport>> SearchDiagnosticReportsAsync(
            ClientConfigurator configurator,
            DiagnosticReportSearchParams p,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new DiagnosticReportSearchParams();

            var limit = SearchExecutor.NormalizeLimit(p.ListReturnLimit);
            var modifier = p.UseIterateModifier ? IncludeModifier.Iterate : IncludeModifier.None;

            Builder<DiagnosticReport> MakeBaseBuilder()
            {
                var builder = new Builder<DiagnosticReport>();

                if (!string.IsNullOrWhiteSpace(p.Id))
                    builder.With("_id", p.Id);
                if (!string.IsNullOrWhiteSpace(p.Patient))
                    builder.With("patient", p.Patient);
                if (!string.IsNullOrWhiteSpace(p.Category))
                    builder.With("category", p.Category);
                if (!string.IsNullOrWhiteSpace(p.Code))
                    builder.With("code", p.Code);
                if (!string.IsNullOrWhiteSpace(p.Performer))
                    builder.With("performer", p.Performer);

                if (p.IssuedStart.HasValue)
                    builder.With("issued", $"ge{p.IssuedStart.Value:O}");
                if (p.IssuedEnd.HasValue)
                    builder.With("issued", $"le{p.IssuedEnd.Value:O}");

                if (p.Includes is not null && p.Includes.Any())
                    builder.Include(p.Includes, modifier: modifier);
                else
                    builder.Include(DiagnosticReportInclude.Patient);

                if (limit != int.MaxValue)
                    builder.WithCount(limit);
                return builder;
            }

            var fanOuts = new List<FanOutSearchHelper.FanOutParam>();
            if (p.Statuses is { Count: > 0 })
                fanOuts.Add(new FanOutSearchHelper.FanOutParam("status",
                    p.Statuses.Select(s => DiagnosticReportStatusToToken(s)).ToList()));

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
        /// Returns diagnostic reports for a specific patient.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="patient">Patient reference or id.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<DiagnosticReport>> ByPatientAsync(
            ClientConfigurator configurator,
            string patient,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<DiagnosticReportInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new DiagnosticReportSearchParams
            {
                Patient = patient,
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchDiagnosticReportsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns a single diagnostic report by its logical id.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="id">Logical id of the resource.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<DiagnosticReport>> ByIdAsync(
            ClientConfigurator configurator,
            string id,
            IEnumerable<DiagnosticReportInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new DiagnosticReportSearchParams
            {
                Id = id,
                ListReturnLimit = 1,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchDiagnosticReportsAsync(configurator, p, ct);
        }
    }
}
