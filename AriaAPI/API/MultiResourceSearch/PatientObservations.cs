// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.API.IdentityResolvers;
using AriaAPI.Core;
using AriaAPI.Networking.Core;
using AriaAPI.Networking.Helpers;
using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AriaAPI.API.MultiResourceSearch
{
    /// <summary>
    /// Provides a multi-resource search helper that retrieves a <see cref="Patient"/> and their
    /// associated <see cref="Observation"/> resources filtered by date range.
    /// </summary>
    public static partial class MultiResourceSearch
    {
        /// <summary>
        /// Retrieves a patient by identifier and returns the patient resource along with all
        /// <see cref="Observation"/> resources within the specified inclusive date window.
        /// </summary>
        /// <param name="configurator">Client configurator providing resource clients and authentication.</param>
        /// <param name="patientIdentifier">Patient MRN or FHIR <c>system|value</c> identifier.</param>
        /// <param name="start">Inclusive start of the observation date range.</param>
        /// <param name="end">Inclusive end of the observation date range.</param>
        /// <param name="listReturnLimit">
        /// Defensive cap on the number of returned <see cref="Observation"/> resources.
        /// Values &lt;= 0 are treated as unbounded (<see cref="int.MaxValue"/>).
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A tuple of the resolved <see cref="Patient"/> (or <see langword="null"/> if not found)
        /// and the list of matching <see cref="Observation"/> resources.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configurator"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="patientIdentifier"/> is null or whitespace.</exception>
        public static async Task<(Patient? patient, List<Observation> observations)> PatientAndObservationsByDateAsync(
            ClientConfigurator configurator,
            string patientIdentifier,
            DateTimeOffset start,
            DateTimeOffset end,
            int listReturnLimit = -1,
            CancellationToken ct = default)
        {
            if (configurator is null) throw new ArgumentNullException(nameof(configurator));
            Ensure.NotNullOrWhiteSpace(patientIdentifier, nameof(patientIdentifier));

            if (listReturnLimit <= 0) listReturnLimit = int.MaxValue;

            var patient = await ResolvePatientAsync(configurator, patientIdentifier, ct).ConfigureAwait(false);
            if (patient is null)
                return (null, new List<Observation>());

            var patientId = patient.Id ?? throw new InvalidOperationException(
                $"Patient resolved for identifier '{PhiMask.Mask(patientIdentifier)}' has a null or empty Id.");

            var observationClient = configurator.ForResource<Observation>(ct);
            var obsBuilder = new Builder<Observation>()
                .ForPatient(patientId)
                .With("date", $"ge{start:O}")
                .With("date", $"le{end:O}");
            if (listReturnLimit != int.MaxValue) obsBuilder.WithCount(listReturnLimit);

            var observations = await observationClient.AggregateResourcesAsync(obsBuilder.Build()).ConfigureAwait(false);
            return (patient, observations);
        }
    }
}
