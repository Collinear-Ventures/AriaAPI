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
    /// Provides search operations for FHIR <see cref="Observation"/> resources using
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
    ///   <item><term><c>based-on</c></term><description>Reference to the plan, proposal or order that is fulfilled.</description></item>
    ///   <item><term><c>category</c></term><description>Observation category code (token).</description></item>
    ///   <item><term><c>code</c></term><description>Observation code (token).</description></item>
    ///   <item><term><c>date</c></term><description>Obtained date/time (date search; supports prefixes/ranges per server).</description></item>
    ///   <item><term><c>focus</c></term><description>Who or what is the focus of the observation (Reference).</description></item>
    ///   <item><term><c>patient</c></term><description>Subject of the observation is a patient (Reference or logical id token).</description></item>
    ///   <item><term><c>performer</c></term><description>Who performed the observation (Reference).</description></item>
    ///   <item><term><c>status</c></term><description>Status of the observation (<c>preliminary</c> | <c>final</c> | <c>entered-in-error</c>).</description></item>
    /// </list>
    ///
    /// Includes:
    /// <list type="bullet">
    ///   <item><description><c>_include=Observation:based-on</c></description></item>
    ///   <item><description><c>_include=Observation:patient</c></description></item>
    ///   <item><description><c>_include=Observation:performer</c></description></item>
    /// </list>
    /// </remarks>
    public static class ObservationSearch
    {

        /// <summary>
        /// Encapsulates search parameters for Observation queries.
        /// All parameters are optional; supply any combination to constrain results.
        /// </summary>
        public sealed class ObservationSearchParams
        {
            /// <summary>Logical ID of the Observation resource (FHIR <c>_id</c>).</summary>
            public string? Id { get; init; }

            /// <summary>Reference to the plan/order fulfilled (FHIR <c>based-on</c>).</summary>
            public string? BasedOn { get; init; }

            /// <summary>Observation category (token) (FHIR <c>category</c>).</summary>
            public string? Category { get; init; }

            /// <summary>Observation code (token) (FHIR <c>code</c>).</summary>
            public string? Code { get; init; }

            /// <summary>
            /// Obtained date/time (FHIR <c>date</c>).
            /// Supports raw string so callers can send prefixes/ranges (e.g., "ge2024-01-01", "2025", "lt2025-06-01").
            /// </summary>
            public string? Date { get; init; }

            /// <summary>Who/what is the focus (FHIR <c>focus</c> Reference search).</summary>
            public string? Focus { get; init; }

            /// <summary>Subject is a patient (FHIR <c>patient</c> Reference/token).</summary>
            public string? Patient { get; init; }

            /// <summary>Performer of the observation (FHIR <c>performer</c> Reference).</summary>
            public string? Performer { get; init; }

            /// <summary>Status (limited set) (FHIR <c>status</c>).</summary>
            public ObservationStatusShort? Status { get; init; }

            /// <summary>Include <c>Observation:based-on</c>.</summary>
            public bool IncludeBasedOn { get; init; } = false;

            /// <summary>Include <c>Observation:patient</c>.</summary>
            public bool IncludePatient { get; init; } = false;

            /// <summary>Include <c>Observation:performer</c>.</summary>
            public bool IncludePerformer { get; init; } = false;

            /// <summary>Client-side defensive cap (applied after aggregation). Defaults to <c>SearchExecutor.DefaultServerMaxResults</c> (500).</summary>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;
        }

        /// <summary>
        /// Executes an Observation search using the provided parameter bag.
        /// Builds FHIR <see cref="SearchParams"/> via <c>Builder&lt;T&gt;</c> and aggregates results across pages.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="p">Search parameter bag.</param>
        /// <param name="ct">Cancellation token.</param>
        public static async Task<List<Observation>> SearchObservationsAsync(
            ClientConfigurator configurator,
            ObservationSearchParams p,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new ObservationSearchParams();

            var limit = SearchExecutor.NormalizeLimit(p.ListReturnLimit);

            return await SearchExecutor.ExecuteAsync(
                configurator,
                () =>
                {
                    var builder = new Builder<Observation>();

                    // Filters
                    if (!string.IsNullOrWhiteSpace(p.Id))
                        builder.With("_id", p.Id);

                    if (!string.IsNullOrWhiteSpace(p.BasedOn))
                        builder.With("based-on", p.BasedOn);

                    if (!string.IsNullOrWhiteSpace(p.Category))
                        builder.With("category", p.Category);

                    if (!string.IsNullOrWhiteSpace(p.Code))
                        builder.With("code", p.Code);

                    if (!string.IsNullOrWhiteSpace(p.Date))
                        builder.With("date", p.Date);

                    if (!string.IsNullOrWhiteSpace(p.Focus))
                        builder.With("focus", p.Focus);

                    if (!string.IsNullOrWhiteSpace(p.Patient))
                        builder.With("patient", p.Patient);

                    if (!string.IsNullOrWhiteSpace(p.Performer))
                        builder.With("performer", p.Performer);

                    if (p.Status.HasValue)
                        builder.With("status", StatusToToken(p.Status.Value));

                    // Count
                    if (limit > 0 && limit != int.MaxValue)
                        builder.WithCount(limit);

                    // _includes
                    if (p.IncludeBasedOn)
                        builder.Include(ObservationInclude.BasedOn);

                    if (p.IncludePatient)
                        builder.Include(ObservationInclude.Patient);

                    if (p.IncludePerformer)
                        builder.Include(ObservationInclude.Performer);

                    return builder;
                },
                null,
                limit,
                ct).ConfigureAwait(false);
        }

        // -----------------------------
        // Strongly-typed convenience methods
        // -----------------------------

        /// <summary>
        /// Returns Observations by resource ID.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="id">Logical ID of the resource.</param>
        /// <param name="includeBasedOn">Whether to include based-on references.</param>
        /// <param name="includePatient">Whether to include patient references.</param>
        /// <param name="includePerformer">Whether to include performer references.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Observation>> ByIdAsync(
            ClientConfigurator configurator,
            string id,
            bool includeBasedOn = false,
            bool includePatient = false,
            bool includePerformer = false,
            int listReturnLimit = int.MaxValue,
            CancellationToken ct = default)
        {
            var p = new ObservationSearchParams
            {
                Id = id,
                IncludeBasedOn = includeBasedOn,
                IncludePatient = includePatient,
                IncludePerformer = includePerformer,
                ListReturnLimit = listReturnLimit
            };
            return SearchObservationsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns Observations by patient reference.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="patientRefOrId">Patient reference or id.</param>
        /// <param name="includeBasedOn">Whether to include based-on references.</param>
        /// <param name="includePatient">Whether to include patient references.</param>
        /// <param name="includePerformer">Whether to include performer references.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Observation>> ByPatientAsync(
            ClientConfigurator configurator,
            string patientRefOrId,
            bool includeBasedOn = false,
            bool includePatient = false,
            bool includePerformer = false,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new ObservationSearchParams
            {
                Patient = patientRefOrId,
                IncludeBasedOn = includeBasedOn,
                IncludePatient = includePatient,
                IncludePerformer = includePerformer,
                ListReturnLimit = listReturnLimit
            };
            return SearchObservationsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns Observations by status.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="status">Observation status to filter by.</param>
        /// <param name="includeBasedOn">Whether to include based-on references.</param>
        /// <param name="includePatient">Whether to include patient references.</param>
        /// <param name="includePerformer">Whether to include performer references.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Observation>> ByStatusAsync(
            ClientConfigurator configurator,
            ObservationStatusShort status,
            bool includeBasedOn = false,
            bool includePatient = false,
            bool includePerformer = false,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new ObservationSearchParams
            {
                Status = status,
                IncludeBasedOn = includeBasedOn,
                IncludePatient = includePatient,
                IncludePerformer = includePerformer,
                ListReturnLimit = listReturnLimit
            };
            return SearchObservationsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns Observations by code.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="codeToken">Code token to filter by.</param>
        /// <param name="includeBasedOn">Whether to include based-on references.</param>
        /// <param name="includePatient">Whether to include patient references.</param>
        /// <param name="includePerformer">Whether to include performer references.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Observation>> ByCodeAsync(
            ClientConfigurator configurator,
            string codeToken,
            bool includeBasedOn = false,
            bool includePatient = false,
            bool includePerformer = false,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new ObservationSearchParams
            {
                Code = codeToken,
                IncludeBasedOn = includeBasedOn,
                IncludePatient = includePatient,
                IncludePerformer = includePerformer,
                ListReturnLimit = listReturnLimit
            };
            return SearchObservationsAsync(configurator, p, ct);
        }
    }
}
