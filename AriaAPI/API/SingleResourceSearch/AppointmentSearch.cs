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
    /// Provides high-level search helpers for FHIR <see cref="Appointment"/> resources.
    ///
    /// This class builds SearchParams using <see cref="Builder{T}"/>, issues searches via a
    /// <see cref="ClientConfigurator"/>, and exposes convenience methods for common query patterns
    /// (patient-centric searches, time-slicing, includes, adaptive splitting of busy intervals).
    /// </summary>
    public static partial class AppointmentSearch
    {
        /// <summary>
        /// Holds the primary results of an appointment search plus any included resources.
        /// </summary>
        public sealed class AppointmentSearchResult
        {
            /// <summary>
            /// List of appointments returned by the search. May be empty.
            /// </summary>
            public List<Appointment> Appointments { get; }

            /// <summary>
            /// Dictionary of included resources (keyed by relative identity "ResourceType/Id" and
            /// by bundle FullUrl when present). Keys are case-insensitive.
            /// </summary>
            public Dictionary<string, Resource> Included { get; }

            /// <summary>
            /// Create a new instance of <see cref="AppointmentSearchResult"/>.
            /// </summary>
            /// <param name="appointments">Appointments returned by the search.</param>
            /// <param name="included">Included resources indexed by identity/FullUrl.</param>
            public AppointmentSearchResult(
                List<Appointment> appointments,
                Dictionary<string, Resource> included)
            {
                Appointments = appointments;
                Included = included;
            }
        }

        /// <summary>
        /// Parameter bag used by search helpers to describe the desired appointment query.
        /// </summary>
        public sealed class AppointmentSearchParams
        {
            // -----------------------------
            // Core requested parameters
            // -----------------------------

            /// <summary>
            /// FHIR "actor" search parameter. Accepts references such as "Patient/123" or "Practitioner/45".
            /// </summary>
            public List<string>? Actors { get; init; }

            /// <summary>
            /// Range start for the appointment date/time (used with "ge" in built SearchParams).
            /// </summary>
            public DateTimeOffset? Start { get; init; }

            /// <summary>
            /// Range end for the appointment date/time (often used with "le" or "lt").
            /// </summary>
            public DateTimeOffset? End { get; init; }

            /// <summary>
            /// FHIR "identifier" tokens to match appointment identifiers.
            /// </summary>
            public List<string>? Identifiers { get; init; }

            /// <summary>
            /// Vendor-supported "patient" parameter (or normalized patient references).
            /// </summary>
            public List<string>? Patients { get; init; }

            /// <summary>
            /// FHIR "service-category" values (mapped using internal category map).
            /// </summary>
            public List<AppointmentCategory>? ServiceCategories { get; init; }

            /// <summary>
            /// FHIR "service-type" tokens.
            /// </summary>
            public List<string>? ServiceTypes { get; init; }

            /// <summary>
            /// Appointment status filter (maps to FHIR appointment.status token).
            /// </summary>
            public Appointment.AppointmentStatus? Status { get; init; }

            // -----------------------------
            // Existing/quality-of-life knobs
            // -----------------------------

            /// <summary>
            /// Maximum number of appointments to return. Defaults to <see cref="SearchExecutor.DefaultServerMaxResults"/> (500).
            /// Values &lt;= 0 are treated as "no explicit limit".
            /// </summary>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;

            /// <summary>
            /// Optional page size to request from the server (maps to _count). If null, a default is chosen.
            /// </summary>
            public int? PageSize { get; init; }

            /// <summary>
            /// Requested FHIR _include paths for related resources (e.g., Patient, Practitioner).
            /// </summary>
            public IEnumerable<AppointmentInclude>? Includes { get; init; }

            /// <summary>
            /// When true, applies the :iterate modifier to include requests if the server supports it.
            /// </summary>
            public bool UseIterateModifier { get; init; } = false;

            /// <summary>
            /// Optional list of element names to request via _elements when not requesting includes.
            /// </summary>
            public IEnumerable<string>? Elements { get; init; }

            /// <summary>
            /// Optional summary mode for the search (e.g., "data", "text", "count", "true").
            /// </summary>
            public string? Summary { get; init; } // "true" | "text" | "data" | "count" | null

            /// <summary>
            /// Known-good element names that can be safely requested via _elements when supported.
            /// </summary>
            public static readonly HashSet<string> AppointmentAllowedElements = new(StringComparer.OrdinalIgnoreCase)
            {
                "id","meta","status","start","end","serviceType","serviceCategory","description",
                "created","minutesDuration","slot","comment","participant","reasonCode",
                "supportingInformation","basedOn"
            };
        }

        /// <summary>
        /// Executes a search for <see cref="Appointment"/> resources using the provided parameter bag.
        /// Builds FHIR SearchParams with a <see cref="Builder{T}"/>, sends the query using a configured
        /// Appointment client, and aggregates results across pages.
        /// </summary>
        /// <param name="configurator">Client configurator used to construct a resource client.</param>
        /// <param name="p">Search parameter bag. When null, a default instance is used.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>List of appointments matching the query (de-duplicated by the server/page aggregation).</returns>
        /// <exception cref="ArgumentNullException"><paramref name="configurator"/> is null.</exception>
        public static async Task<List<Appointment>> SearchAppointmentsAsync(
            ClientConfigurator configurator,
            AppointmentSearchParams p,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new AppointmentSearchParams();

            var limit = p.ListReturnLimit <= 0 ? int.MaxValue : p.ListReturnLimit;
            var pageSize = p.PageSize ?? Math.Min(limit, SearchExecutor.DefaultPageSize);

            Builder<Appointment> MakeBaseBuilder()
            {
                var builder = new Builder<Appointment>();

                if (p.Start.HasValue)
                    builder.With("date", $"ge{p.Start.Value:O}");
                if (p.End.HasValue)
                    builder.With("date", $"le{p.End.Value:O}");

                if (p.Status.HasValue)
                    builder.With("status", p.Status.Value.ToString().ToLowerInvariant());

                // -----------------------------
                // _include
                // -----------------------------
                var modifier = p.UseIterateModifier ? IncludeModifier.Iterate : IncludeModifier.None;
                if (p.Includes is not null && p.Includes.Any())
                    builder.Include(p.Includes, modifier: modifier);

                if (!string.IsNullOrWhiteSpace(p.Summary))
                    builder.With("_summary", p.Summary);

                builder.WithCount(pageSize);

                return builder;
            }

            return await SearchExecutor.ExecuteAsync(
                configurator,
                MakeBaseBuilder,
                BuildAppointmentFanOutParams(p),
                limit,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes an appointment search that preserves included resources and returns them alongside appointments.
        /// This variant pages the server and indexes included resources by identity and FullUrl.
        /// </summary>
        /// <param name="configurator">Client configurator used to build the Appointment client.</param>
        /// <param name="p">Search parameters. When null, a default instance is used.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// An <see cref="AppointmentSearchResult"/> containing the appointments and any included resources.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="configurator"/> is null.</exception>
        public static async Task<AppointmentSearchResult> SearchAppointmentsWithIncludesAsync(
            ClientConfigurator configurator,
            AppointmentSearchParams p,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new AppointmentSearchParams();

            var apptClient = configurator.ForResource<Appointment>(ct);
            var limit = p.ListReturnLimit <= 0 ? int.MaxValue : p.ListReturnLimit;
            var pageSize = p.PageSize ?? Math.Min(limit, SearchExecutor.DefaultServerMaxResults);

            Builder<Appointment> MakeBaseBuilder()
            {
                var builder = new Builder<Appointment>();

                if (p.Start.HasValue) builder.With("date", $"ge{p.Start.Value:O}");
                if (p.End.HasValue) builder.With("date", $"le{p.End.Value:O}");

                if (p.Status.HasValue)
                    builder.With("status", p.Status.Value.ToString().ToLowerInvariant());

                // -----------------------------
                // _include (+ :iterate if requested)
                // -----------------------------
                var modifier = p.UseIterateModifier ? IncludeModifier.Iterate : IncludeModifier.None;
                if (p.Includes is not null && p.Includes.Any())
                    builder.Include(p.Includes, modifier: modifier);

                if (p.Elements is { } els && els.Any()
                    && (p.Includes is null || !p.Includes.Any())
                    && !p.UseIterateModifier)
                {
                    foreach (var el in els.Where(s => !string.IsNullOrWhiteSpace(s)))
                        builder.With("_elements", el);
                }

                builder.WithCount(pageSize);
                return builder;
            }

            async Task<AppointmentSearchResult> ExecuteWithIncludes(SearchParams sp)
            {
                var appts = new List<Appointment>();
                var included = new Dictionary<string, Resource>(StringComparer.OrdinalIgnoreCase);

                var bundle = await apptClient.SearchBundleAsync(sp).ConfigureAwait(false);
                while (bundle is not null)
                {
                    IndexBundle(bundle, included);
                    appts.AddRange(bundle.Entry
                        .Where(e => e.Resource is Appointment)
                        .Select(e => (Appointment)e.Resource!));
                    if (appts.Count >= limit) break;
                    bundle = await apptClient.ContinueAsync(bundle).ConfigureAwait(false);
                }

                if (appts.Count > limit)
                    appts = appts.Take(limit).ToList();

                return new AppointmentSearchResult(appts, included);
            }

            var fanOuts = BuildAppointmentFanOutParams(p);
            return await FanOutSearchHelper.FanOutSearchAsync<Appointment, AppointmentSearchResult>(
                sp => ExecuteWithIncludes(sp),
                results => MergeAppointmentResults(results),
                r => r.Appointments.Where(a => !string.IsNullOrWhiteSpace(a.Id)).Select(a => a.Id!),
                (r, ids) => new AppointmentSearchResult(
                    r.Appointments.Where(a => !string.IsNullOrWhiteSpace(a.Id) && ids.Contains(a.Id!)).ToList(),
                    r.Included),
                MakeBaseBuilder,
                fanOuts,
                ct: ct).ConfigureAwait(false);
        }
    }
}
