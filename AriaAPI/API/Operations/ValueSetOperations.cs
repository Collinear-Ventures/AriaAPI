// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.Core;
using AriaAPI.Networking.Helpers;
using Hl7.Fhir.Model;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AriaAPI.API.Operations
{
    /// <summary>
    /// Provides FHIR type-level operations for <see cref="ValueSet"/> resources.
    /// </summary>
    public static class ValueSetOperations
    {
        /// <summary>
        /// Invokes the <c>ValueSet/$expand</c> FHIR operation to expand a value set by its canonical URL.
        /// </summary>
        /// <param name="configurator">FHIR client configurator.</param>
        /// <param name="valueSetUrl">
        /// Canonical URL of the <see cref="ValueSet"/> to expand
        /// (e.g., <c>http://varian.com/fhir/ValueSet/my-vs</c>).
        /// </param>
        /// <param name="filter">
        /// Optional filter string to restrict expansion results (FHIR <c>filter</c> parameter).
        /// </param>
        /// <param name="count">Optional maximum number of codes to return in the expansion.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// The expanded <see cref="ValueSet"/> with <c>ValueSet.Expansion</c> populated,
        /// or <see langword="null"/> if the operation returns no result.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configurator"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="valueSetUrl"/> is null or whitespace.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the server returns a Bundle but the entry does not contain a <see cref="ValueSet"/>.
        /// </exception>
        /// <remarks>
        /// The Firely SDK's <c>TypeOperationAsync&lt;ValueSet&gt;</c> returns a <see cref="Bundle"/> whose
        /// first entry contains the expanded <see cref="ValueSet"/>. This method unwraps that entry.
        /// No PHI is involved in ValueSet expansion — no masking is applied.
        /// </remarks>
        public static async Task<ValueSet?> ExpandAsync(
            ClientConfigurator configurator,
            string valueSetUrl,
            string? filter = null,
            int? count = null,
            CancellationToken ct = default)
        {
            if (configurator is null) throw new ArgumentNullException(nameof(configurator));
            Ensure.NotNullOrWhiteSpace(valueSetUrl, nameof(valueSetUrl));

            // Build Parameters resource
            var parms = new Parameters();
            parms.Add("url", new FhirString(valueSetUrl));
            if (!string.IsNullOrWhiteSpace(filter))
                parms.Add("filter", new FhirString(filter));
            if (count.HasValue)
                parms.Add("count", new Integer(count.Value));

            // Invoke the type-level $expand operation
            // TypeOperationAsync returns a Bundle; the ValueSet is in Entry[0].Resource
            var result = await configurator.FhirClient.TypeOperationAsync<ValueSet>(
                operationName: "expand",
                parameters: parms,
                useGet: true
            ).ConfigureAwait(false);

            // Defensive shortcut: if SDK returns a bare ValueSet (not wrapped in Bundle),
            // accept it directly. This handles potential SDK version behaviour changes without
            // breaking the spec's primary Bundle-unwrap path that follows.
            if (result is ValueSet vs)
                return vs;

            var bundle = result as Bundle
                ?? throw new InvalidOperationException(
                    "ValueSet/$expand did not return a ValueSet or a Bundle.");

            var entry = bundle.Entry?.FirstOrDefault()?.Resource as ValueSet;
            if (entry is null)
                throw new InvalidOperationException(
                    bundle.Entry is null || bundle.Entry.Count == 0
                        ? "ValueSet/$expand returned a Bundle with no entries."
                        : "ValueSet/$expand returned a Bundle but Entry[0] is not a ValueSet.");
            return entry;
        }
    }
}
