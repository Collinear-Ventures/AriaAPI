// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.API.SearchHelpers;
using AriaAPI.Core;
using AriaAPI.Networking.Core;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static AriaAPI.API.SearchHelpers.SearchHelpers;

namespace AriaAPI.API.SingleResourceSearch
{
    /// <summary>
    /// Provides search operations for FHIR BodyStructure resources using ClientConfigurator and Builder&lt;T&gt;.
    /// </summary>
    public static class BodyStructureSearch
    {
        /// <summary>
        /// Encapsulates search parameters for BodyStructure queries.
        /// </summary>
        public sealed class BodyStructureSearchParams
        {
            /// <summary>
            /// The logical ID of the BodyStructure resource (_id).
            /// </summary>
            public string? Id { get; init; }

            /// <summary>
            /// Identifier assigned to the BodyStructure (e.g., external system ID).
            /// </summary>
            public string? Identifier { get; init; }

            /// <summary>
            /// Reference to the patient this BodyStructure is about (e.g., Patient/{id}).
            /// </summary>
            public string? Patient { get; init; }

            /// <summary>
            /// Maximum number of BodyStructure resources to return. Defaults to <c>SearchExecutor.DefaultServerMaxResults</c> (500).
            /// </summary>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;
        }

        /// <summary>
        /// Executes a flexible BodyStructure search using the provided parameter bag.
        /// Builds FHIR SearchParams via Builder&lt;T&gt; and aggregates results across pages.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="p">Search parameter bag containing ID, identifier, patient, and limits.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A list of BodyStructure resources matching the criteria.</returns>
        public static async Task<List<BodyStructure>> SearchBodyStructuresAsync(
            ClientConfigurator configurator,
            BodyStructureSearchParams p,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new BodyStructureSearchParams();

            var limit = SearchExecutor.NormalizeLimit(p.ListReturnLimit);

            return await SearchExecutor.ExecuteAsync(
                configurator,
                () =>
                {
                    var builder = new Builder<BodyStructure>();

                    if (!string.IsNullOrWhiteSpace(p.Id))
                        builder.With("_id", p.Id);

                    if (!string.IsNullOrWhiteSpace(p.Identifier))
                        builder.With("identifier", p.Identifier);

                    if (!string.IsNullOrWhiteSpace(p.Patient))
                        builder.With("patient", p.Patient);

                    builder.WithCount(limit);
                    return builder;
                },
                null,
                limit,
                ct).ConfigureAwait(false);
        }

        // -----------------------------
        // Strongly typed convenience methods
        // -----------------------------

        /// <summary>
        /// Returns BodyStructures by resource ID.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="id">Logical ID of the resource.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<BodyStructure>> ByIdAsync(
            ClientConfigurator configurator,
            string id,
            int listReturnLimit = int.MaxValue,
            CancellationToken ct = default)
        {
            var p = new BodyStructureSearchParams
            {
                Id = id,
                ListReturnLimit = listReturnLimit
            };
            return SearchBodyStructuresAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns BodyStructures by identifier.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="identifier">Identifier to filter by.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<BodyStructure>> ByIdentifierAsync(
            ClientConfigurator configurator,
            string identifier,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new BodyStructureSearchParams
            {
                Identifier = identifier,
                ListReturnLimit = listReturnLimit
            };
            return SearchBodyStructuresAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns BodyStructures for a specific patient.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="patientReference">Patient reference or id.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<BodyStructure>> ByPatientAsync(
            ClientConfigurator configurator,
            string patientReference,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new BodyStructureSearchParams
            {
                Patient = patientReference,
                ListReturnLimit = listReturnLimit
            };
            return SearchBodyStructuresAsync(configurator, p, ct);
        }
    }
}
