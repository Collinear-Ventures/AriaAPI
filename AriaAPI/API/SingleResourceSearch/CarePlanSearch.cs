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

namespace AriaAPI.API.SingleResourceSearch
{
    /// <summary>
    /// Provides search operations for FHIR CarePlan resources using ClientConfigurator and Builder&lt;T&gt;.
    /// </summary>
    public static class CarePlanSearch
    {
        /// <summary>
        /// Encapsulates search parameters for CarePlan queries.
        /// </summary>
        public sealed class CarePlanSearchParams
        {
            /// <summary>
            /// Logical ID of the CarePlan resource (_id).
            /// </summary>
            public string? Id { get; init; }

            /// <summary>
            /// Filters CarePlans where the specified date occurs within
            /// CarePlan.activity.detail.scheduled[x] (FHIR search: activity-date).
            /// </summary>
            public DateTimeOffset? ActivityDate { get; init; }

            /// <summary>
            /// Reference to the patient this CarePlan is about (e.g., Patient/{id}).
            /// </summary>
            public string? Patient { get; init; }

            /// <summary>
            /// Title of the CarePlan (FHIR search: title).
            /// </summary>
            public string? Title { get; init; }

            /// <summary>
            /// FHIR _include paths for related resources (e.g., Patient, Practitioner).
            /// If null, defaults to including Patient.
            /// </summary>
            public IEnumerable<CarePlanInclude>? Includes { get; init; }

            /// <summary>
            /// Apply :iterate modifier to includes if supported by the server.
            /// </summary>
            public bool UseIterateModifier { get; init; } = false;

            /// <summary>
            /// Maximum number of CarePlans to return. Defaults to <c>SearchExecutor.DefaultServerMaxResults</c> (500).
            /// </summary>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;
        }

        /// <summary>
        /// Executes a flexible CarePlan search using the provided parameter bag.
        /// Builds FHIR SearchParams via Builder&lt;T&gt; and aggregates results across pages.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="p">Search parameter bag containing ID, activity-date, patient, title, includes, and limits.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A list of CarePlan resources matching the criteria.</returns>
        public static async Task<List<CarePlan>> SearchCarePlansAsync(
            ClientConfigurator configurator,
            CarePlanSearchParams p,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new CarePlanSearchParams();

            var limit = SearchExecutor.NormalizeLimit(p.ListReturnLimit);

            return await SearchExecutor.ExecuteAsync(
                configurator,
                () =>
                {
                    var builder = new Builder<CarePlan>();

                    if (!string.IsNullOrWhiteSpace(p.Id))
                        builder.With("_id", p.Id);

                    if (p.ActivityDate.HasValue)
                        builder.With("activity-date", p.ActivityDate.Value.ToString("O"));

                    if (!string.IsNullOrWhiteSpace(p.Patient))
                        builder.With("patient", p.Patient);

                    if (!string.IsNullOrWhiteSpace(p.Title))
                        builder.With("title", p.Title);

                    //var modifier = p.UseIterateModifier ? IncludeModifier.Iterate : IncludeModifier.None;
                    var modifier = IncludeModifier.None;
                    if (p.Includes is not null && p.Includes.Any())
                    {
                        builder.Include(p.Includes, modifier: modifier);
                    }
                    else
                    {
                        builder.Include(CarePlanInclude.Patient); // Default include
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
        /// Returns CarePlans by resource ID.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="id">Logical ID of the resource.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<CarePlan>> ByIdAsync(
            ClientConfigurator configurator,
            string id,
            int listReturnLimit = int.MaxValue,
            CancellationToken ct = default)
        {
            var p = new CarePlanSearchParams
            {
                Id = id,
                ListReturnLimit = listReturnLimit
            };
            return SearchCarePlansAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns CarePlans for a specific patient.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="patientReference">Patient reference or id.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<CarePlan>> ByPatientAsync(
            ClientConfigurator configurator,
            string patientReference,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new CarePlanSearchParams
            {
                Patient = patientReference,
                ListReturnLimit = listReturnLimit
            };
            return SearchCarePlansAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns CarePlans matching a title.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="title">Title to search for.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<CarePlan>> ByTitleAsync(
            ClientConfigurator configurator,
            string title,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new CarePlanSearchParams
            {
                Title = title,
                ListReturnLimit = listReturnLimit
            };
            return SearchCarePlansAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns CarePlans where the specified activity date occurs within scheduled[x].
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="activityDate">Activity date to filter by.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<CarePlan>> ByActivityDateAsync(
            ClientConfigurator configurator,
            DateTimeOffset activityDate,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new CarePlanSearchParams
            {
                ActivityDate = activityDate,
                ListReturnLimit = listReturnLimit
            };
            return SearchCarePlansAsync(configurator, p, ct);
        }
    }
}
