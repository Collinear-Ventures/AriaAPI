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
    /// Provides search operations for FHIR <see cref="Schedule"/> resources using
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
    ///   <item><term><c>actor</c></term><description>The individual(HealthcareService, Practitioner, Location, ...) to find a Schedule for.</description></item>
    ///   <item><term><c>service-type</c></term><description>The type of appointments that can be booked into associated slot(s) (token).</description></item>
    ///   <item><term><c>date</c></term><description>Schedule date range (<c>ge</c>/<c>le</c> prefixes).</description></item>
    ///   <item><term><c>active</c></term><description>Is the schedule in active use.</description></item>
    /// </list>
    /// Includes are supported via <see cref="ScheduleInclude"/> and default to <see cref="ScheduleInclude.Actor"/>.
    /// </remarks>
    public static class ScheduleSearch
    {
        /// <summary>
        /// Encapsulates search parameters for <see cref="Schedule"/> queries.
        /// </summary>
        public sealed class ScheduleSearchParams
        {
            /// <summary>Logical ID of the Schedule resource (FHIR <c>_id</c>).</summary>
            public string? Id { get; init; }

            /// <summary>The individual to find a Schedule for (reference or id).</summary>
            public string? Actor { get; init; }

            /// <summary>The type of appointments that can be booked (token).</summary>
            public string? ServiceType { get; init; }

            /// <summary>Inclusive start boundary for schedule date (<c>date</c> with <c>ge</c>).</summary>
            public DateTimeOffset? DateStart { get; init; }

            /// <summary>Inclusive end boundary for schedule date (<c>date</c> with <c>le</c>).</summary>
            public DateTimeOffset? DateEnd { get; init; }

            /// <summary>Whether the schedule is in active use.</summary>
            public bool? Active { get; init; }

            /// <summary>
            /// FHIR _include paths for related resources.
            /// If null, defaults to including <see cref="ScheduleInclude.Actor"/>.
            /// </summary>
            public IEnumerable<ScheduleInclude>? Includes { get; init; }

            /// <summary>Apply <c>:iterate</c> modifier to includes if supported by the server.</summary>
            public bool UseIterateModifier { get; init; } = false;

            /// <summary>
            /// Maximum number of Schedule resources to return client-side.
            /// Defaults to <c>SearchExecutor.DefaultServerMaxResults</c> (500).
            /// </summary>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;
        }

        /// <summary>
        /// Executes a flexible Schedule search using the provided parameter bag.
        /// Builds FHIR <see cref="SearchParams"/> via <c>Builder&lt;T&gt;</c> and aggregates results across pages.
        /// </summary>
        /// <param name="configurator">The <see cref="ClientConfigurator"/> used to create a resource-specific client.</param>
        /// <param name="p">Search parameter bag.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A deduplicated list of <see cref="Schedule"/> resources matching the criteria.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="configurator"/> is <c>null</c>.</exception>
        public static async Task<List<Schedule>> SearchSchedulesAsync(
            ClientConfigurator configurator,
            ScheduleSearchParams p,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new ScheduleSearchParams();

            var limit = SearchExecutor.NormalizeLimit(p.ListReturnLimit);
            var modifier = p.UseIterateModifier ? IncludeModifier.Iterate : IncludeModifier.None;

            Builder<Schedule> MakeBaseBuilder()
            {
                var builder = new Builder<Schedule>();

                if (!string.IsNullOrWhiteSpace(p.Id))
                    builder.With("_id", p.Id);
                if (!string.IsNullOrWhiteSpace(p.Actor))
                    builder.With("actor", p.Actor);
                if (!string.IsNullOrWhiteSpace(p.ServiceType))
                    builder.With("service-type", p.ServiceType);
                if (p.Active.HasValue)
                    builder.With("active", p.Active.Value ? "true" : "false");

                if (p.DateStart.HasValue)
                    builder.With("date", $"ge{p.DateStart.Value:O}");
                if (p.DateEnd.HasValue)
                    builder.With("date", $"le{p.DateEnd.Value:O}");

                if (p.Includes is not null && p.Includes.Any())
                    builder.Include(p.Includes, modifier: modifier);
                else
                    builder.Include(ScheduleInclude.Actor);

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
        /// Returns schedules for a specific actor.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="actor">Actor reference or id.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Schedule>> ByActorAsync(
            ClientConfigurator configurator,
            string actor,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<ScheduleInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new ScheduleSearchParams
            {
                Actor = actor,
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchSchedulesAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns a single schedule by its logical id.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="id">Logical id of the resource.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Schedule>> ByIdAsync(
            ClientConfigurator configurator,
            string id,
            IEnumerable<ScheduleInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new ScheduleSearchParams
            {
                Id = id,
                ListReturnLimit = 1,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchSchedulesAsync(configurator, p, ct);
        }
    }
}
