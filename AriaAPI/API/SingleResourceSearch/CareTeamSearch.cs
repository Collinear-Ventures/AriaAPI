// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.API.SearchHelpers;
using AriaAPI.Core;
using AriaAPI.Networking.Core;
using AriaAPI.Networking.Helpers;
using AriaAPI.Resources.Includes;
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
    /// Provides search operations for FHIR <see cref="CareTeam"/> resources using
    /// <see cref="ClientConfigurator"/> and <c>Builder&lt;T&gt;</c>.
    /// </summary>
    /// <remarks>
    /// Supports flexible queries with optional includes for related resources:
    /// <list type="bullet">
    ///   <item><description>Patient</description></item>
    ///   <item><description>Practitioner</description></item>
    ///   <item><description>Organization</description></item>
    /// </list>
    /// </remarks>
    public static class CareTeamSearch
    {
        /// <summary>
        /// Encapsulates search parameters for <see cref="CareTeam"/> queries.
        /// </summary>
        public sealed class CareTeamSearchParams
        {
            /// <summary>
            /// Logical ID of the CareTeam resource (<c>_id</c>).
            /// </summary>
            public string? Id { get; init; }

            /// <summary>
            /// Participant reference or identifier (FHIR search parameter <c>participant</c>).
            /// </summary>
            public string? Participant { get; init; }

            /// <summary>
            /// Patient reference (FHIR search parameter <c>patient</c>).
            /// </summary>
            public string? Patient { get; init; }

            /// <summary>
            /// FHIR _include paths for related resources (e.g., Patient, Practitioner, Organization).
            /// If null, defaults to including Patient.
            /// </summary>
            public IEnumerable<CareTeamInclude>? Includes { get; init; }

            /// <summary>
            /// Apply :iterate modifier to includes if supported by the server.
            /// </summary>
            public bool UseIterateModifier { get; init; } = false;

            /// <summary>
            /// Maximum number of CareTeam resources to return. Defaults to <c>SearchExecutor.DefaultServerMaxResults</c> (500).
            /// </summary>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;
        }

        /// <summary>
        /// Executes a flexible CareTeam search using the provided parameter bag.
        /// Builds FHIR <see cref="SearchParams"/> via <c>Builder&lt;T&gt;</c> and aggregates results across pages.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="p">Search parameter bag containing ID, participant, patient, includes, and limits.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A list of CareTeam resources matching the criteria.</returns>
        public static async Task<List<CareTeam>> SearchCareTeamsAsync(
            ClientConfigurator configurator,
            CareTeamSearchParams p,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new CareTeamSearchParams();

            var limit = SearchExecutor.NormalizeLimit(p.ListReturnLimit);

            return await SearchExecutor.ExecuteAsync(
                configurator,
                () =>
                {
                    var builder = new Builder<CareTeam>();

                    if (!string.IsNullOrWhiteSpace(p.Id))
                        builder.With("_id", p.Id);

                    if (!string.IsNullOrWhiteSpace(p.Participant))
                        builder.With("participant", p.Participant);

                    if (!string.IsNullOrWhiteSpace(p.Patient))
                        builder.With("patient", p.Patient);

                    //var modifier = p.UseIterateModifier ? IncludeModifier.Iterate : IncludeModifier.None;
                    var modifier = IncludeModifier.None;
                    if (p.Includes is not null && p.Includes.Any())
                    {
                        builder.Include(p.Includes, modifier: modifier);
                    }
                    else
                    {
                        // Default include: Patient
                        builder.Include("CareTeam:patient");
                    }

                    if (limit != int.MaxValue)
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
        /// Returns CareTeams by resource ID.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="id">Logical ID of the resource.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<CareTeam>> ByIdAsync(ClientConfigurator configurator, string id, int listReturnLimit = -1, CancellationToken ct = default)
        {
            var p = new CareTeamSearchParams
            {
                Id = id,
                ListReturnLimit = listReturnLimit < 0 ? int.MaxValue : listReturnLimit
            };
            return SearchCareTeamsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns CareTeams by participant reference.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="participant">Participant reference or id.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<CareTeam>> ByParticipantAsync(ClientConfigurator configurator, string participant, int listReturnLimit = SearchExecutor.DefaultServerMaxResults, CancellationToken ct = default)
        {
            var p = new CareTeamSearchParams
            {
                Participant = participant,
                ListReturnLimit = listReturnLimit < 0 ? int.MaxValue : listReturnLimit
            };
            return SearchCareTeamsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns CareTeams for a specific patient.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="patientReference">Patient reference or id.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<CareTeam>> ByPatientAsync(ClientConfigurator configurator, string patientReference, int listReturnLimit = SearchExecutor.DefaultServerMaxResults, CancellationToken ct = default)
        {
            var p = new CareTeamSearchParams
            {
                Patient = patientReference,
                ListReturnLimit = listReturnLimit < 0 ? int.MaxValue : listReturnLimit
            };
            return SearchCareTeamsAsync(configurator, p, ct);
        }
    }
}
