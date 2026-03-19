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
using static AriaAPI.API.SearchHelpers.SearchTypes;

namespace AriaAPI.API.SingleResourceSearch
{
    public static partial class AppointmentSearch
    {
        /// <summary>
        /// Represents a closed-open time slice [From, To) used by slicing helpers.
        /// </summary>
        private readonly record struct TimeSlice(DateTimeOffset From, DateTimeOffset To);

        /// <summary>
        /// Result wrapper that includes page fetching statistics used by adaptive slicing.
        /// </summary>
        public sealed record AppointmentSearchResultWithStats(
            AppointmentSearchResult Result,
            int PagesFetched,
            int AppointmentCount);

        /// <summary>
        /// Index resources present in a bundle into the provided dictionary.
        /// Keys used: "ResourceType/Id" (when available) and bundle FullUrl (when present).
        /// </summary>
        /// <param name="b">Bundle to index.</param>
        /// <param name="index">Dictionary to populate (case-insensitive keys).</param>
        private static void IndexBundle(Bundle b, Dictionary<string, Resource> index)
        {
            if (b.Entry is null) return;

            foreach (var e in b.Entry)
            {
                var r = e.Resource;
                if (r is null) continue;

                // Key by relative identity: "ResourceType/Id"
                if (!string.IsNullOrWhiteSpace(r.Id))
                {
                    var key = $"{r.TypeName}/{r.Id}";
                    index[key] = r;
                }

                // Also key by FullUrl if present
                if (!string.IsNullOrWhiteSpace(e.FullUrl))
                    index[e.FullUrl] = r;
            }
        }

        /// <summary>
        /// Normalize a patient identifier or reference into a full reference string.
        /// Example: "123" -> "Patient/123"; "Patient/123" remains unchanged.
        /// </summary>
        private static string NormalizePatientRef(string patientIdOrRef)
        {
            if (string.IsNullOrWhiteSpace(patientIdOrRef)) return patientIdOrRef;
            return patientIdOrRef.Contains("/") ? patientIdOrRef : $"Patient/{patientIdOrRef}";
        }

        /// <summary>
        /// Yield a sequence of contiguous time windows that partition [startInclusive, endExclusive)
        /// using the provided slice size. The final slice may be shorter.
        /// </summary>
        private static IEnumerable<(DateTimeOffset From, DateTimeOffset To)> SliceRange(
            DateTimeOffset startInclusive,
            DateTimeOffset endExclusive,
            TimeSpan sliceSize)
        {
            for (var t = startInclusive; t < endExclusive; t = t.Add(sliceSize))
            {
                var next = t.Add(sliceSize);
                if (next > endExclusive) next = endExclusive;
                yield return (t, next);
            }
        }

        /// <summary>
        /// Split a timeslice into two halves. Preserves the original From offset when constructing the midpoint.
        /// </summary>
        private static TimeSlice[] SplitInHalf(TimeSlice s)
        {
            var midTicks = s.From.UtcTicks + ((s.To.UtcTicks - s.From.UtcTicks) / 2);
            var mid = new DateTimeOffset(new DateTime(midTicks, DateTimeKind.Utc));
            // Convert mid back to the same offset as From to keep formatting stable
            mid = mid.ToOffset(s.From.Offset);

            return new[]
            {
                new TimeSlice(s.From, mid),
                new TimeSlice(mid, s.To)
            };
        }

        /// <summary>
        /// Merges multiple <see cref="AppointmentSearchResult"/> values from fan-out queries into a single result.
        /// Appointments are deduplicated by <see cref="Hl7.Fhir.Model.Resource.Id"/> using last-write-wins semantics
        /// (later results overwrite earlier ones for the same Id). Included resources are merged the same way.
        /// </summary>
        private static AppointmentSearchResult MergeAppointmentResults(IReadOnlyList<AppointmentSearchResult> results)
        {
            var apptById = new Dictionary<string, Appointment>(StringComparer.OrdinalIgnoreCase);
            var included = new Dictionary<string, Resource>(StringComparer.OrdinalIgnoreCase);
            foreach (var r in results)
            {
                foreach (var a in r.Appointments)
                    if (!string.IsNullOrWhiteSpace(a.Id)) apptById[a.Id] = a;
                foreach (var kv in r.Included)
                    included[kv.Key] = kv.Value;
            }
            return new AppointmentSearchResult(apptById.Values.ToList(), included);
        }

        /// <summary>
        /// Builds the list of fan-out parameters from the appointment search param bag.
        /// Multi-valued parameters (actors, patients, identifiers, service-categories)
        /// are fanned out as separate queries and their results unioned/intersected.
        /// </summary>
        private static IReadOnlyList<FanOutSearchHelper.FanOutParam> BuildAppointmentFanOutParams(
            AppointmentSearchParams p)
        {
            var fanOuts = new List<FanOutSearchHelper.FanOutParam>();

            if (p.Actors is { Count: > 0 })
                fanOuts.Add(new FanOutSearchHelper.FanOutParam("actor", p.Actors));

            if (p.Patients is { Count: > 0 })
                fanOuts.Add(new FanOutSearchHelper.FanOutParam("patient", p.Patients));

            if (p.Identifiers is { Count: > 0 })
                fanOuts.Add(new FanOutSearchHelper.FanOutParam("identifier", p.Identifiers));

            if (p.ServiceCategories is { Count: > 0 })
            {
                var categoryValues = p.ServiceCategories
                    .Select(c => GetSearchValue(c, AppointmentCategoryMap))
                    .ToList();
                fanOuts.Add(new FanOutSearchHelper.FanOutParam("service-category", categoryValues));
            }

            if (p.ServiceTypes is { Count: > 0 })
                fanOuts.Add(new FanOutSearchHelper.FanOutParam("service-type", p.ServiceTypes));

            return fanOuts;
        }

        /// <summary>
        /// Executes a search for a single slice and returns statistics including pages fetched.
        /// Internal helper used by adaptive slicing routines.
        /// </summary>
        internal static async Task<AppointmentSearchResultWithStats> SearchAppointmentsWithIncludesAndStatsAsync(
            ClientConfigurator configurator,
            AppointmentSearchParams p,
            CancellationToken ct = default)
        {
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

                var modifier = p.UseIterateModifier ? IncludeModifier.Iterate : IncludeModifier.None;
                if (p.Includes is not null && p.Includes.Any())
                    builder.Include(p.Includes, modifier: modifier);

                if (!string.IsNullOrWhiteSpace(p.Summary))
                    builder.With("_summary", p.Summary);

                builder.With("_total", "none");
                builder.With("_sort", "StartTime");
                builder.WithCount(pageSize);
                return builder;
            }

            async Task<AppointmentSearchResultWithStats> ExecuteWithStats(SearchParams sp)
            {
                var appts = new List<Appointment>();
                var included = new Dictionary<string, Resource>(StringComparer.OrdinalIgnoreCase);
                int pagesFetched = 0;

                var bundle = await apptClient.SearchBundleAsync(sp).ConfigureAwait(false);
                while (bundle is not null)
                {
                    pagesFetched++;
                    IndexBundle(bundle, included);
                    appts.AddRange(bundle.Entry
                        .Where(e => e.Resource is Appointment)
                        .Select(e => (Appointment)e.Resource!));
                    if (appts.Count >= limit) break;
                    bundle = await apptClient.ContinueAsync(bundle).ConfigureAwait(false);
                }

                if (appts.Count > limit)
                    appts = appts.Take(limit).ToList();

                var result = new AppointmentSearchResult(appts, included);
                return new AppointmentSearchResultWithStats(result, pagesFetched, appts.Count);
            }

            var fanOuts = BuildAppointmentFanOutParams(p);
            return await FanOutSearchHelper.FanOutSearchAsync<Appointment, AppointmentSearchResultWithStats>(
                sp => ExecuteWithStats(sp),
                results =>
                {
                    var merged = MergeAppointmentResults(results.Select(r => r.Result).ToList());
                    return new AppointmentSearchResultWithStats(
                        merged,
                        results.Sum(r => r.PagesFetched),
                        merged.Appointments.Count);
                },
                r => r.Result.Appointments.Where(a => !string.IsNullOrWhiteSpace(a.Id)).Select(a => a.Id!),
                (r, ids) =>
                {
                    var filtered = new AppointmentSearchResult(
                        r.Result.Appointments.Where(a => !string.IsNullOrWhiteSpace(a.Id) && ids.Contains(a.Id!)).ToList(),
                        r.Result.Included);
                    return new AppointmentSearchResultWithStats(filtered, r.PagesFetched, filtered.Appointments.Count);
                },
                MakeBaseBuilder,
                fanOuts,
                ct: ct).ConfigureAwait(false);
        }
    }
}
