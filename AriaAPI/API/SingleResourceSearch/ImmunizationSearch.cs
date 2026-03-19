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
    /// Provides search operations for FHIR <see cref="Immunization"/> resources using
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
    ///   <item><term><c>status</c></term><description>Immunization status (completed | entered-in-error | not-done).</description></item>
    ///   <item><term><c>vaccine-code</c></term><description>Vaccine product administered (token).</description></item>
    ///   <item><term><c>date</c></term><description>Vaccination administration date (<c>ge</c>/<c>le</c> prefixes).</description></item>
    /// </list>
    /// Includes are supported via <see cref="ImmunizationInclude"/> and default to <see cref="ImmunizationInclude.Patient"/>.
    /// </remarks>
    public static class ImmunizationSearch
    {
        /// <summary>
        /// Encapsulates search parameters for <see cref="Immunization"/> queries.
        /// </summary>
        public sealed class ImmunizationSearchParams
        {
            /// <summary>Logical ID of the Immunization resource (FHIR <c>_id</c>).</summary>
            public string? Id { get; init; }

            /// <summary>Patient reference or id.</summary>
            public string? Patient { get; init; }

            /// <summary>
            /// One or more immunization status tokens for filtering.
            /// Use raw FHIR values: <c>completed</c>, <c>entered-in-error</c>, or <c>not-done</c>.
            /// </summary>
            public List<string>? Statuses { get; init; }

            /// <summary>Vaccine product administered (token).</summary>
            public string? VaccineCode { get; init; }

            /// <summary>Inclusive start boundary for administration date (<c>date</c> with <c>ge</c>).</summary>
            public DateTimeOffset? DateStart { get; init; }

            /// <summary>Inclusive end boundary for administration date (<c>date</c> with <c>le</c>).</summary>
            public DateTimeOffset? DateEnd { get; init; }

            /// <summary>
            /// FHIR _include paths for related resources.
            /// If null, defaults to including <see cref="ImmunizationInclude.Patient"/>.
            /// </summary>
            public IEnumerable<ImmunizationInclude>? Includes { get; init; }

            /// <summary>Apply <c>:iterate</c> modifier to includes if supported by the server.</summary>
            public bool UseIterateModifier { get; init; } = false;

            /// <summary>
            /// Maximum number of Immunization resources to return client-side.
            /// Defaults to <c>SearchExecutor.DefaultServerMaxResults</c> (500).
            /// </summary>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;
        }

        /// <summary>
        /// Executes a flexible Immunization search using the provided parameter bag.
        /// The <c>Statuses</c> list is fanned out into individual FHIR queries and aggregated
        /// via <see cref="FanOutSearchHelper"/>.
        /// </summary>
        /// <param name="configurator">The <see cref="ClientConfigurator"/> used to create a resource-specific client.</param>
        /// <param name="p">Search parameter bag.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A deduplicated list of <see cref="Immunization"/> resources matching the criteria.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="configurator"/> is <c>null</c>.</exception>
        public static async Task<List<Immunization>> SearchImmunizationsAsync(
            ClientConfigurator configurator,
            ImmunizationSearchParams p,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new ImmunizationSearchParams();

            var limit = SearchExecutor.NormalizeLimit(p.ListReturnLimit);
            var modifier = p.UseIterateModifier ? IncludeModifier.Iterate : IncludeModifier.None;

            Builder<Immunization> MakeBaseBuilder()
            {
                var builder = new Builder<Immunization>();

                if (!string.IsNullOrWhiteSpace(p.Id))
                    builder.With("_id", p.Id);
                if (!string.IsNullOrWhiteSpace(p.Patient))
                    builder.With("patient", p.Patient);
                if (!string.IsNullOrWhiteSpace(p.VaccineCode))
                    builder.With("vaccine-code", p.VaccineCode);

                if (p.DateStart.HasValue)
                    builder.With("date", $"ge{p.DateStart.Value:O}");
                if (p.DateEnd.HasValue)
                    builder.With("date", $"le{p.DateEnd.Value:O}");

                if (p.Includes is not null && p.Includes.Any())
                    builder.Include(p.Includes, modifier: modifier);
                else
                    builder.Include(ImmunizationInclude.Patient);

                if (limit != int.MaxValue)
                    builder.WithCount(limit);
                return builder;
            }

            var fanOuts = new List<FanOutSearchHelper.FanOutParam>();
            if (p.Statuses is { Count: > 0 })
            {
                var values = p.Statuses.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                if (values.Count > 0)
                    fanOuts.Add(new FanOutSearchHelper.FanOutParam("status", values));
            }

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
        /// Returns immunizations for a specific patient.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="patient">Patient reference or id.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Immunization>> ByPatientAsync(
            ClientConfigurator configurator,
            string patient,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<ImmunizationInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new ImmunizationSearchParams
            {
                Patient = patient,
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchImmunizationsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns a single immunization by its logical id.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="id">Logical id of the resource.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Immunization>> ByIdAsync(
            ClientConfigurator configurator,
            string id,
            IEnumerable<ImmunizationInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new ImmunizationSearchParams
            {
                Id = id,
                ListReturnLimit = 1,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchImmunizationsAsync(configurator, p, ct);
        }
    }
}
