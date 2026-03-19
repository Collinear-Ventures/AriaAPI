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
    /// Provides search operations for FHIR <see cref="Slot"/> resources using
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
    ///   <item><term><c>schedule</c></term><description>The Schedule Resource that we are seeking a slot within.</description></item>
    ///   <item><term><c>status</c></term><description>Slot status (busy | free | busy-unavailable | busy-tentative | entered-in-error).</description></item>
    ///   <item><term><c>service-type</c></term><description>The type of appointments that can be booked into the slot (token).</description></item>
    ///   <item><term><c>start</c></term><description>Appointment date/time we are looking for slots in a date range (<c>ge</c>/<c>le</c> prefixes).</description></item>
    /// </list>
    /// Includes are supported via <see cref="SlotInclude"/> and default to <see cref="SlotInclude.Schedule"/>.
    /// </remarks>
    public static class SlotSearch
    {
        /// <summary>
        /// Encapsulates search parameters for <see cref="Slot"/> queries.
        /// </summary>
        public sealed class SlotSearchParams
        {
            /// <summary>Logical ID of the Slot resource (FHIR <c>_id</c>).</summary>
            public string? Id { get; init; }

            /// <summary>Schedule reference or id.</summary>
            public string? Schedule { get; init; }

            /// <summary>
            /// One or more slot status tokens for filtering.
            /// Use raw FHIR values: <c>busy</c>, <c>free</c>, <c>busy-unavailable</c>, <c>busy-tentative</c>, or <c>entered-in-error</c>.
            /// </summary>
            public List<string>? Statuses { get; init; }

            /// <summary>The type of appointments that can be booked into the slot (token).</summary>
            public string? ServiceType { get; init; }

            /// <summary>Inclusive start boundary for slot start time (<c>start</c> with <c>ge</c>).</summary>
            public DateTimeOffset? StartStart { get; init; }

            /// <summary>Inclusive end boundary for slot start time (<c>start</c> with <c>le</c>).</summary>
            public DateTimeOffset? StartEnd { get; init; }

            /// <summary>
            /// FHIR _include paths for related resources.
            /// If null, defaults to including <see cref="SlotInclude.Schedule"/>.
            /// </summary>
            public IEnumerable<SlotInclude>? Includes { get; init; }

            /// <summary>Apply <c>:iterate</c> modifier to includes if supported by the server.</summary>
            public bool UseIterateModifier { get; init; } = false;

            /// <summary>
            /// Maximum number of Slot resources to return client-side.
            /// Defaults to <c>SearchExecutor.DefaultServerMaxResults</c> (500).
            /// </summary>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;
        }

        /// <summary>
        /// Executes a flexible Slot search using the provided parameter bag.
        /// The <c>Statuses</c> list is fanned out into individual FHIR queries and aggregated
        /// via <see cref="FanOutSearchHelper"/>.
        /// </summary>
        /// <param name="configurator">The <see cref="ClientConfigurator"/> used to create a resource-specific client.</param>
        /// <param name="p">Search parameter bag.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A deduplicated list of <see cref="Slot"/> resources matching the criteria.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="configurator"/> is <c>null</c>.</exception>
        public static async Task<List<Slot>> SearchSlotsAsync(
            ClientConfigurator configurator,
            SlotSearchParams p,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new SlotSearchParams();

            var limit = SearchExecutor.NormalizeLimit(p.ListReturnLimit);
            var modifier = p.UseIterateModifier ? IncludeModifier.Iterate : IncludeModifier.None;

            Builder<Slot> MakeBaseBuilder()
            {
                var builder = new Builder<Slot>();

                if (!string.IsNullOrWhiteSpace(p.Id))
                    builder.With("_id", p.Id);
                if (!string.IsNullOrWhiteSpace(p.Schedule))
                    builder.With("schedule", p.Schedule);
                if (!string.IsNullOrWhiteSpace(p.ServiceType))
                    builder.With("service-type", p.ServiceType);

                if (p.StartStart.HasValue)
                    builder.With("start", $"ge{p.StartStart.Value:O}");
                if (p.StartEnd.HasValue)
                    builder.With("start", $"le{p.StartEnd.Value:O}");

                if (p.Includes is not null && p.Includes.Any())
                    builder.Include(p.Includes, modifier: modifier);
                else
                    builder.Include(SlotInclude.Schedule);

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
        /// Returns slots for a specific schedule.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="schedule">Schedule reference or id.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Slot>> ByScheduleAsync(
            ClientConfigurator configurator,
            string schedule,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<SlotInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new SlotSearchParams
            {
                Schedule = schedule,
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchSlotsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns a single slot by its logical id.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="id">Logical id of the resource.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Slot>> ByIdAsync(
            ClientConfigurator configurator,
            string id,
            IEnumerable<SlotInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new SlotSearchParams
            {
                Id = id,
                ListReturnLimit = 1,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchSlotsAsync(configurator, p, ct);
        }
    }
}
