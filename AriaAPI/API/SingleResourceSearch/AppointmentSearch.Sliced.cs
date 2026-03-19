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
    public static partial class AppointmentSearch
    {
        /// <summary>
        /// Time-sliced parallel search that executes independent slices and merges results.
        /// Each slice is fetched in parallel with a concurrency limit and then merged de-duplicating by Id.
        /// </summary>
        /// <param name="aria">Client configurator to create resource clients from.</param>
        /// <param name="baseParams">Base search parameters; must include <see cref="AppointmentSearchParams.Start"/> and <see cref="AppointmentSearchParams.End"/>.</param>
        /// <param name="sliceSize">Duration for each slice created from the Start..End window.</param>
        /// <param name="maxConcurrency">Maximum number of concurrently executing slice fetch tasks.</param>
        /// <param name="ct">Cancellation token that cancels work started by this method.</param>
        /// <returns>Merged search results containing appointments and included resources.</returns>
        /// <exception cref="ArgumentException">When Start or End are not specified in <paramref name="baseParams"/>.</exception>
        public static async Task<AppointmentSearch.AppointmentSearchResult> SearchAppointmentsWithIncludesTimeSlicedAsync(
            ClientConfigurator aria,
            AppointmentSearch.AppointmentSearchParams baseParams,
            TimeSpan sliceSize,
            int maxConcurrency = 4,
            CancellationToken ct = default)
        {
            if (baseParams.Start is null || baseParams.End is null)
                throw new ArgumentException("Start/End are required for time-slicing.");

            var slices = SliceRange(baseParams.Start.Value, baseParams.End.Value, sliceSize).ToList();

            // throttle parallelism (be kind to ARIA)
            using var sem = new SemaphoreSlim(maxConcurrency);

            var tasks = slices.Select(async slice =>
            {
                await sem.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    // clone params but narrow the date window to this slice
                    var pSlice = new AppointmentSearch.AppointmentSearchParams
                    {
                        Start = slice.From,
                        End = slice.To,

                        Actors = baseParams.Actors,
                        Identifiers = baseParams.Identifiers,
                        Patients = baseParams.Patients,
                        ServiceCategories = baseParams.ServiceCategories,
                        ServiceTypes = baseParams.ServiceTypes,
                        Status = baseParams.Status,

                        Includes = baseParams.Includes,
                        UseIterateModifier = baseParams.UseIterateModifier,

                        PageSize = baseParams.PageSize,
                        ListReturnLimit = baseParams.ListReturnLimit,

                        Summary = baseParams.Summary,
                        Elements = baseParams.Elements
                    };

                    return await SearchAppointmentsWithIncludesAsync(aria, pSlice, ct)
                                                  .ConfigureAwait(false);
                }
                finally
                {
                    sem.Release();
                }
            }).ToList();

            var results = await System.Threading.Tasks.Task.WhenAll(tasks).ConfigureAwait(false);

            // Merge + de-dupe
            var apptById = new Dictionary<string, Appointment>(StringComparer.OrdinalIgnoreCase);
            var included = new Dictionary<string, Resource>(StringComparer.OrdinalIgnoreCase);

            foreach (var r in results)
            {
                foreach (var a in r.Appointments)
                {
                    if (!string.IsNullOrWhiteSpace(a.Id))
                        apptById[a.Id] = a;
                }

                foreach (var kv in r.Included)
                    included[kv.Key] = kv.Value;
            }

            return new AppointmentSearch.AppointmentSearchResult(apptById.Values.ToList(), included);
        }

        /// <summary>
        /// Adaptive fetch for a single time slice. If the slice requires too many pages, it will be split
        /// and fetched recursively (respecting <paramref name="minSlice"/>).
        /// </summary>
        private static async Task<AppointmentSearchResultWithStats> FetchSliceAdaptiveAsync(
            ClientConfigurator cfg,
            AppointmentSearchParams baseParams,
            TimeSlice slice,
            TimeSpan minSlice,
            int pageThreshold,
            SemaphoreSlim throttler,
            CancellationToken ct)
        {
            // clone params with slice bounds
            var pSlice = new AppointmentSearchParams
            {
                Start = slice.From,
                End = slice.To,

                Actors = baseParams.Actors,
                Identifiers = baseParams.Identifiers,
                Patients = baseParams.Patients,
                ServiceCategories = baseParams.ServiceCategories,
                ServiceTypes = baseParams.ServiceTypes,
                Status = baseParams.Status,

                Includes = baseParams.Includes,
                UseIterateModifier = baseParams.UseIterateModifier,
                Summary = baseParams.Summary,
                Elements = baseParams.Elements,

                PageSize = baseParams.PageSize,
                ListReturnLimit = baseParams.ListReturnLimit
            };

            await throttler.WaitAsync(ct).ConfigureAwait(false);
            AppointmentSearchResultWithStats stats;
            try
            {
                stats = await SearchAppointmentsWithIncludesAndStatsAsync(cfg, pSlice, ct).ConfigureAwait(false);
            }
            finally
            {
                throttler.Release();
            }

            var duration = slice.To - slice.From;

            // If the slice paged too much, and we can still split, split & recurse
            if (stats.PagesFetched > pageThreshold && duration > minSlice)
            {
                var halves = SplitInHalf(slice);

                // Recurse in parallel (bounded by throttler)
                var leftTask = FetchSliceAdaptiveAsync(cfg, baseParams, halves[0], minSlice, pageThreshold, throttler, ct);
                var rightTask = FetchSliceAdaptiveAsync(cfg, baseParams, halves[1], minSlice, pageThreshold, throttler, ct);

                var both = await System.Threading.Tasks.Task.WhenAll(leftTask, rightTask).ConfigureAwait(false);

                return MergeStats(both[0], both[1]);
            }

            return stats;
        }

        /// <summary>
        /// Merge two slice results: de-duplicate appointments by Id and combine included resources.
        /// </summary>
        private static AppointmentSearchResultWithStats MergeStats(
            AppointmentSearchResultWithStats a,
            AppointmentSearchResultWithStats b)
        {
            var apptById = new Dictionary<string, Appointment>(StringComparer.OrdinalIgnoreCase);
            foreach (var appt in a.Result.Appointments)
                if (!string.IsNullOrWhiteSpace(appt.Id)) apptById[appt.Id] = appt;
            foreach (var appt in b.Result.Appointments)
                if (!string.IsNullOrWhiteSpace(appt.Id)) apptById[appt.Id] = appt;

            var included = new Dictionary<string, Resource>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in a.Result.Included) included[kv.Key] = kv.Value;
            foreach (var kv in b.Result.Included) included[kv.Key] = kv.Value;

            var mergedResult = new AppointmentSearchResult(apptById.Values.ToList(), included);

            return new AppointmentSearchResultWithStats(
                mergedResult,
                PagesFetched: a.PagesFetched + b.PagesFetched,
                AppointmentCount: mergedResult.Appointments.Count);
        }

        /// <summary>
        /// Performs a parallel, time-sliced search for Appointment resources across the specified
        /// Start..End window in <paramref name="p"/>.
        ///
        /// This "hot-sliced" strategy:
        /// - Partitions the full time range into fixed-size slices of <paramref name="initialSlice"/>.
        /// - Fetches each slice in parallel, bounded by <paramref name="maxConcurrency"/>.
        /// - Uses an adaptive recursive splitter (via <see cref="FetchSliceAdaptiveAsync"/>) to
        ///   subdivide any slice that requires more than <paramref name="pageThreshold"/> pages,
        ///   down to <paramref name="minSlice"/>, to avoid excessive paging work for very busy intervals.
        /// - Merges and de-duplicates results across slices (by resource Id) before returning.
        /// </summary>
        /// <param name="cfg">Client configurator used to create an Appointment FHIR client.</param>
        /// <param name="p">Search parameters. Must include non-null <see cref="AppointmentSearchParams.Start"/> and <see cref="AppointmentSearchParams.End"/>.</param>
        /// <param name="initialSlice">Initial slice duration used to partition the time window. Each slice covers [inclusive, exclusive).</param>
        /// <param name="minSlice">Smallest slice duration allowed when adaptively splitting busy slices. Slices shorter than this will not be split further.</param>
        /// <param name="maxConcurrency">Maximum number of slice fetches to run concurrently. Defaults to 4.</param>
        /// <param name="pageThreshold">
        /// Threshold of pages for a slice; if a slice requires more than this number of pages to fetch,
        /// it will be split and fetched recursively. Defaults to 1 (i.e., split if more than one page).
        /// </param>
        /// <param name="ct">Cancellation token to cancel the operation and any in-flight slice fetches.</param>
        /// <returns>
        /// A task that resolves to an <see cref="AppointmentSearchResult"/> containing merged appointments
        /// and included resources. Appointments are de-duplicated by Id; included resources are preserved
        /// and keyed by relative identity and FullUrl.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="p"/> does not contain both Start and End.</exception>
        /// <remarks>
        /// - Cancellation via <paramref name="ct"/> is propagated to underlying fetch calls.
        /// - The method is intended for large date ranges where parallelism and adaptive splitting
        ///   help avoid servers returning very large bundles or many pages for a single query window.
        /// - Tuning knobs:
        ///     * <paramref name="initialSlice"/>: coarse-grain partitioning (smaller -> more tasks).
        ///     * <paramref name="minSlice"/>: lower bound for adaptive splitting (prevents infinite split).
        ///     * <paramref name="pageThreshold"/>: sensitivity for splitting busy slices.
        ///     * <paramref name="maxConcurrency"/>: throttles concurrent requests to be kind to the server.
        /// </remarks>
        public static async Task<AppointmentSearchResult> SearchAppointmentsHotSlicedAsync(
            ClientConfigurator cfg,
            AppointmentSearchParams p,
            TimeSpan initialSlice,
            TimeSpan minSlice,
            int maxConcurrency = 4,
            int pageThreshold = 1,
            CancellationToken ct = default)
        {
            if (p.Start is null || p.End is null)
                throw new ArgumentException("Start and End are required.");

            var slices = new List<TimeSlice>();
            for (var t = p.Start.Value; t < p.End.Value; t = t.Add(initialSlice))
            {
                var next = t.Add(initialSlice);
                if (next > p.End.Value) next = p.End.Value;
                slices.Add(new TimeSlice(t, next));
            }

            using var throttler = new SemaphoreSlim(maxConcurrency);

            var tasks = slices.Select(s =>
                FetchSliceAdaptiveAsync(cfg, p, s, minSlice, pageThreshold, throttler, ct));

            var results = await System.Threading.Tasks.Task.WhenAll(tasks).ConfigureAwait(false);

            // Merge all slice results
            var merged = results.Aggregate(MergeStats);

            return merged.Result;
        }
    }
}
