// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.Core;
using AriaAPI.Networking.Core;
using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AriaAPI.API.SearchHelpers
{
    /// <summary>
    /// Centralises the repeated search boilerplate that appears in every FHIR single-resource
    /// search class: obtaining a typed client, invoking fan-out, and trimming results to the
    /// caller's requested limit.
    /// </summary>
    internal static class SearchExecutor
    {
        /// <summary>Default page size requested from the FHIR server when not otherwise specified.</summary>
        internal const int DefaultPageSize = 200;

        /// <summary>Conservative upper bound on server-side result counts per request.</summary>
        internal const int DefaultServerMaxResults = 500;

        /// <summary>
        /// Normalizes a caller-supplied result limit. Values &lt;= 0 are treated as unbounded
        /// (<see cref="int.MaxValue"/>).
        /// </summary>
        /// <param name="limit">The raw limit value supplied by the caller.</param>
        /// <returns>
        /// <see cref="int.MaxValue"/> when <paramref name="limit"/> is zero or negative;
        /// otherwise <paramref name="limit"/> unchanged.
        /// </returns>
        internal static int NormalizeLimit(int limit) =>
            limit <= 0 ? int.MaxValue : limit;

        /// <summary>
        /// Executes a FHIR search using the provided builder factory and optional fan-out parameters,
        /// then trims the result to <paramref name="limit"/> entries.
        /// </summary>
        /// <typeparam name="TResource">The FHIR resource type being searched.</typeparam>
        /// <param name="configurator">
        /// The <see cref="ClientConfigurator"/> used to obtain a typed FHIR client.
        /// </param>
        /// <param name="builderFactory">
        /// Factory that returns a fresh <see cref="Builder{TResource}"/> pre-populated with
        /// scalar search parameters. Called once per individual query.
        /// </param>
        /// <param name="fanOuts">
        /// Optional collection of fan-out parameter descriptors. When <see langword="null"/>,
        /// an empty sequence is used (single-query fast path).
        /// </param>
        /// <param name="limit">
        /// Maximum number of resources to return. Use <see cref="int.MaxValue"/> (or any value
        /// &lt;= 0 via <see cref="NormalizeLimit"/>) for no limit.
        /// </param>
        /// <param name="ct">Cancellation token, propagated through fan-out and all internal HTTP calls.</param>
        /// <returns>
        /// A deduplicated list of matching resources, trimmed to at most <paramref name="limit"/>
        /// entries.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="configurator"/> or <paramref name="builderFactory"/> is
        /// <see langword="null"/>.
        /// </exception>
        internal static async Task<List<TResource>> ExecuteAsync<TResource>(
            ClientConfigurator configurator,
            Func<Builder<TResource>> builderFactory,
            IEnumerable<FanOutSearchHelper.FanOutParam>? fanOuts = null,
            int limit = int.MaxValue,
            CancellationToken ct = default)
            where TResource : Resource
        {
            ArgumentNullException.ThrowIfNull(configurator);
            ArgumentNullException.ThrowIfNull(builderFactory);

            ct.ThrowIfCancellationRequested();

            var client = configurator.ForResource<TResource>(ct);

            // List<T> implements IReadOnlyList<T> directly — no .AsReadOnly() wrapper needed.
            var fanOutList = (fanOuts ?? Enumerable.Empty<FanOutSearchHelper.FanOutParam>()).ToList();

            var results = await FanOutSearchHelper.FanOutSearchAsync(client, builderFactory, fanOutList, ct: ct)
                .ConfigureAwait(false);

            if (results.Count > limit)
                results = results.Take(limit).ToList();

            return results;
        }
    }
}
