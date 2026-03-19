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
    /// Provides search operations for FHIR <see cref="ImagingStudy"/> resources using
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
    ///   <item><term><c>status</c></term><description>Study status (registered | available | cancelled | entered-in-error | unknown).</description></item>
    ///   <item><term><c>modality</c></term><description>The modality of the study (token).</description></item>
    ///   <item><term><c>started</c></term><description>When the study was started (<c>ge</c>/<c>le</c> prefixes).</description></item>
    /// </list>
    /// Includes are supported via <see cref="ImagingStudyInclude"/> and default to <see cref="ImagingStudyInclude.Patient"/>.
    /// </remarks>
    public static class ImagingStudySearch
    {
        /// <summary>
        /// Encapsulates search parameters for <see cref="ImagingStudy"/> queries.
        /// </summary>
        public sealed class ImagingStudySearchParams
        {
            /// <summary>Logical ID of the ImagingStudy resource (FHIR <c>_id</c>).</summary>
            public string? Id { get; init; }

            /// <summary>Patient reference or id.</summary>
            public string? Patient { get; init; }

            /// <summary>One or more study statuses for filtering.</summary>
            public List<ImagingStudyStatus>? Statuses { get; init; }

            /// <summary>The modality of the study (token, e.g., <c>CT</c>).</summary>
            public string? Modality { get; init; }

            /// <summary>Inclusive start boundary for study start date (<c>started</c> with <c>ge</c>).</summary>
            public DateTimeOffset? StartedStart { get; init; }

            /// <summary>Inclusive end boundary for study start date (<c>started</c> with <c>le</c>).</summary>
            public DateTimeOffset? StartedEnd { get; init; }

            /// <summary>
            /// FHIR _include paths for related resources.
            /// If null, defaults to including <see cref="ImagingStudyInclude.Patient"/>.
            /// </summary>
            public IEnumerable<ImagingStudyInclude>? Includes { get; init; }

            /// <summary>Apply <c>:iterate</c> modifier to includes if supported by the server.</summary>
            public bool UseIterateModifier { get; init; } = false;

            /// <summary>
            /// Maximum number of ImagingStudy resources to return client-side.
            /// Defaults to <c>SearchExecutor.DefaultServerMaxResults</c> (500).
            /// </summary>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;
        }

        /// <summary>
        /// Executes a flexible ImagingStudy search using the provided parameter bag.
        /// The <c>Statuses</c> list is fanned out into individual FHIR queries and aggregated
        /// via <see cref="FanOutSearchHelper"/>.
        /// </summary>
        /// <param name="configurator">The <see cref="ClientConfigurator"/> used to create a resource-specific client.</param>
        /// <param name="p">Search parameter bag.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A deduplicated list of <see cref="ImagingStudy"/> resources matching the criteria.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="configurator"/> is <c>null</c>.</exception>
        public static async Task<List<ImagingStudy>> SearchImagingStudiesAsync(
            ClientConfigurator configurator,
            ImagingStudySearchParams p,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new ImagingStudySearchParams();

            var limit = SearchExecutor.NormalizeLimit(p.ListReturnLimit);
            var modifier = p.UseIterateModifier ? IncludeModifier.Iterate : IncludeModifier.None;

            Builder<ImagingStudy> MakeBaseBuilder()
            {
                var builder = new Builder<ImagingStudy>();

                if (!string.IsNullOrWhiteSpace(p.Id))
                    builder.With("_id", p.Id);
                if (!string.IsNullOrWhiteSpace(p.Patient))
                    builder.With("patient", p.Patient);
                if (!string.IsNullOrWhiteSpace(p.Modality))
                    builder.With("modality", p.Modality);

                if (p.StartedStart.HasValue)
                    builder.With("started", $"ge{p.StartedStart.Value:O}");
                if (p.StartedEnd.HasValue)
                    builder.With("started", $"le{p.StartedEnd.Value:O}");

                if (p.Includes is not null && p.Includes.Any())
                    builder.Include(p.Includes, modifier: modifier);
                else
                    builder.Include(ImagingStudyInclude.Patient);

                if (limit != int.MaxValue)
                    builder.WithCount(limit);
                return builder;
            }

            var fanOuts = new List<FanOutSearchHelper.FanOutParam>();
            if (p.Statuses is { Count: > 0 })
                fanOuts.Add(new FanOutSearchHelper.FanOutParam("status",
                    p.Statuses.Select(s => ImagingStudyStatusToToken(s)).ToList()));

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
        /// Returns imaging studies for a specific patient.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="patient">Patient reference or id.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<ImagingStudy>> ByPatientAsync(
            ClientConfigurator configurator,
            string patient,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<ImagingStudyInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new ImagingStudySearchParams
            {
                Patient = patient,
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchImagingStudiesAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns a single imaging study by its logical id.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="id">Logical id of the resource.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<ImagingStudy>> ByIdAsync(
            ClientConfigurator configurator,
            string id,
            IEnumerable<ImagingStudyInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new ImagingStudySearchParams
            {
                Id = id,
                ListReturnLimit = 1,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchImagingStudiesAsync(configurator, p, ct);
        }
    }
}
