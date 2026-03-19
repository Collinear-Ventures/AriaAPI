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
    /// Provides search operations for FHIR <see cref="Procedure"/> resources using
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
    ///   <item><term><c>category</c></term><description>Procedure category code (token).</description></item>
    ///   <item><term><c>code</c></term><description>Procedure code (token).</description></item>
    ///   <item><term><c>patient</c></term><description>Subject of the procedure is a patient (Reference or logical id token).</description></item>
    ///   <item><term><c>subject</c></term><description>Who the procedure is performed on (Reference).</description></item>
    /// </list>
    ///
    /// Includes:
    /// <list type="bullet">
    ///   <item><description><c>_include=Procedure:based-on</c></description></item>
    ///   <item><description><c>_include=Procedure:patient</c></description></item>
    /// </list>
    /// </remarks>
    public static class ProcedureSearch
    {
        /// <summary>
        /// Encapsulates search parameters for Procedure queries.
        /// </summary>
        public sealed class ProcedureSearchParams
        {
            /// <summary>Logical ID of the Procedure resource.</summary>
            public string? Id { get; init; }

            /// <summary>Procedure category code (token).</summary>
            public string? Category { get; init; }

            /// <summary>Procedure code (token).</summary>
            public string? Code { get; init; }

            /// <summary>Patient reference or id.</summary>
            public string? Patient { get; init; }

            /// <summary>Subject reference.</summary>
            public string? Subject { get; init; }

            /// <summary>Include Procedure:based-on.</summary>
            public bool IncludeBasedOn { get; init; } = false;

            /// <summary>Include Procedure:patient.</summary>
            public bool IncludePatient { get; init; } = false;

            /// <summary>Client-side defensive cap. Defaults to <c>SearchExecutor.DefaultServerMaxResults</c> (500).</summary>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;
        }

        /// <summary>
        /// Executes a Procedure search using the provided parameter bag.
        /// Builds FHIR <see cref="SearchParams"/> via <c>Builder&lt;T&gt;</c> and aggregates results across pages.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="p">Search parameter bag.</param>
        /// <param name="ct">Cancellation token.</param>
        public static async Task<List<Procedure>> SearchProceduresAsync(
            ClientConfigurator configurator,
            ProcedureSearchParams p,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new ProcedureSearchParams();

            var limit = SearchExecutor.NormalizeLimit(p.ListReturnLimit);

            return await SearchExecutor.ExecuteAsync(
                configurator,
                () =>
                {
                    var builder = new Builder<Procedure>();

                    if (!string.IsNullOrWhiteSpace(p.Id))
                        builder.With("_id", p.Id);

                    if (!string.IsNullOrWhiteSpace(p.Category))
                        builder.With("category", p.Category);

                    if (!string.IsNullOrWhiteSpace(p.Code))
                        builder.With("code", p.Code);

                    if (!string.IsNullOrWhiteSpace(p.Patient))
                        builder.With("patient", p.Patient);

                    if (!string.IsNullOrWhiteSpace(p.Subject))
                        builder.With("subject", p.Subject);

                    if (limit > 0 && limit != int.MaxValue)
                        builder.WithCount(limit);

                    if (p.IncludeBasedOn)
                        builder.Include(ProcedureInclude.BasedOn);

                    if (p.IncludePatient)
                        builder.Include(ProcedureInclude.Patient);

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
        /// Returns Procedures by resource ID.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="id">Logical ID of the resource.</param>
        /// <param name="includeBasedOn">Whether to include based-on references.</param>
        /// <param name="includePatient">Whether to include patient references.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Procedure>> ByIdAsync(
            ClientConfigurator configurator,
            string id,
            bool includeBasedOn = false,
            bool includePatient = false,
            int listReturnLimit = int.MaxValue,
            CancellationToken ct = default)
        {
            var p = new ProcedureSearchParams
            {
                Id = id,
                IncludeBasedOn = includeBasedOn,
                IncludePatient = includePatient,
                ListReturnLimit = listReturnLimit
            };
            return SearchProceduresAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns Procedures by patient reference.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="patientRefOrId">Patient reference or id.</param>
        /// <param name="includeBasedOn">Whether to include based-on references.</param>
        /// <param name="includePatient">Whether to include patient references.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Procedure>> ByPatientAsync(
            ClientConfigurator configurator,
            string patientRefOrId,
            bool includeBasedOn = false,
            bool includePatient = false,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new ProcedureSearchParams
            {
                Patient = patientRefOrId,
                IncludeBasedOn = includeBasedOn,
                IncludePatient = includePatient,
                ListReturnLimit = listReturnLimit
            };
            return SearchProceduresAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns Procedures by code.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="codeToken">Code token to filter by.</param>
        /// <param name="includeBasedOn">Whether to include based-on references.</param>
        /// <param name="includePatient">Whether to include patient references.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Procedure>> ByCodeAsync(
            ClientConfigurator configurator,
            string codeToken,
            bool includeBasedOn = false,
            bool includePatient = false,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new ProcedureSearchParams
            {
                Code = codeToken,
                IncludeBasedOn = includeBasedOn,
                IncludePatient = includePatient,
                ListReturnLimit = listReturnLimit
            };
            return SearchProceduresAsync(configurator, p, ct);
        }
    }
}
