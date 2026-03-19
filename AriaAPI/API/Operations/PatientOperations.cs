// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.API.IdentityResolvers;
using AriaAPI.API.SearchHelpers;
using AriaAPI.Core;
using AriaAPI.Networking.Core;
using AriaAPI.Networking.Helpers;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AriaAPI.API.Operations
{
    /// <summary>
    /// Provides FHIR instance-level operations for <see cref="Patient"/> resources.
    /// </summary>
    public static class PatientOperations
    {
        /// <summary>
        /// Default maximum number of resources returned by <see cref="EverythingAsync"/>.
        /// Value matches <c>SearchExecutor.DefaultServerMaxResults</c> (500).
        /// </summary>
        public const int DefaultListReturnLimit = SearchExecutor.DefaultServerMaxResults;

        /// <summary>
        /// Invokes the <c>Patient/$everything</c> FHIR operation to retrieve all resources associated
        /// with a patient, with optional date range filtering and pagination.
        /// </summary>
        /// <param name="configurator">FHIR client configurator.</param>
        /// <param name="patientIdentifier">
        /// Patient MRN or FHIR <c>system|value</c> identifier. Resolved to a logical Patient ID
        /// before invoking the operation.
        /// </param>
        /// <param name="start">Optional inclusive start date for the operation date range filter.</param>
        /// <param name="end">Optional inclusive end date for the operation date range filter.</param>
        /// <param name="listReturnLimit">
        /// Maximum number of resources to return across all Bundle pages.
        /// Defaults to <see cref="DefaultListReturnLimit"/> (500). Values &lt;= 0 are treated as unbounded.
        /// A <c>Warning</c> is logged if results are trimmed.
        /// </param>
        /// <param name="logger">Optional logger for diagnostic output and PHI-masked audit entries.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// All <see cref="Resource"/> entries from the <c>$everything</c> Bundle, trimmed to
        /// <paramref name="listReturnLimit"/>. Returns an empty list if the patient is not found.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configurator"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="patientIdentifier"/> is null or whitespace.</exception>
        /// <remarks>
        /// The operation URL is constructed as <c>Patient/{id}/$everything</c> and called via
        /// <c>FhirClient.GetAsync</c>. Pagination is followed via <c>FhirClient.ContinueAsync</c>
        /// until <paramref name="listReturnLimit"/> is reached or no next page exists.
        /// PHI is masked in all log output.
        /// </remarks>
        public static async Task<List<Resource>> EverythingAsync(
            ClientConfigurator configurator,
            string patientIdentifier,
            DateTimeOffset? start = null,
            DateTimeOffset? end = null,
            int listReturnLimit = DefaultListReturnLimit,
            ILogger? logger = null,
            CancellationToken ct = default)
        {
            if (configurator is null) throw new ArgumentNullException(nameof(configurator));
            Ensure.NotNullOrWhiteSpace(patientIdentifier, nameof(patientIdentifier));

            if (listReturnLimit <= 0) listReturnLimit = int.MaxValue;

            // Step 1: Resolve patient by identifier
            var patientClient = configurator.ForResource<Patient>(ct);
            var patientParams = new Builder<Patient>().ByIdentifier(patientIdentifier).Build();
            var patientResults = await patientClient.SearchFirstPageAsync(patientParams, pageSize: 1).ConfigureAwait(false);
            var patient = patientResults.FirstOrDefault();

            if (patient is null)
            {
                logger?.LogInformation(
                    "Patient not found for identifier {Id}; returning empty list.",
                    PhiMask.Mask(patientIdentifier));
                return new List<Resource>();
            }

            if (string.IsNullOrWhiteSpace(patient.Id))
                throw new InvalidOperationException(
                    $"Resolved Patient has a null or empty Id for identifier '{PhiMask.Mask(patientIdentifier)}'.");

            logger?.LogInformation(
                "Invoking Patient/$everything for patient {Id}",
                PhiMask.Mask(patient.Id));

            // Step 2: Build URL for $everything
            var url = $"Patient/{patient.Id}/$everything";
            if (start.HasValue) url += $"?start={start.Value:yyyy-MM-dd}";
            if (end.HasValue) url += (start.HasValue ? "&" : "?") + $"end={end.Value:yyyy-MM-dd}";

            // Step 3: Call GetAsync and follow pagination
            var result = await configurator.FhirClient.GetAsync(url, ct).ConfigureAwait(false);
            var bundle = result as Bundle;

            var resources = new List<Resource>();
            while (bundle is not null)
            {
                foreach (var entry in bundle.Entry ?? Enumerable.Empty<Bundle.EntryComponent>())
                {
                    if (entry.Resource is { } r)
                    {
                        resources.Add(r);
                        if (resources.Count >= listReturnLimit)
                        {
                            logger?.LogWarning(
                                "Patient/$everything results trimmed to {Limit} for patient {Id}.",
                                listReturnLimit,
                                PhiMask.Mask(patient.Id));
                            return resources;
                        }
                    }
                }

                // Follow next page
                bundle = await configurator.FhirClient.ContinueAsync(bundle, ct: ct).ConfigureAwait(false);
            }

            return resources;
        }
    }
}
