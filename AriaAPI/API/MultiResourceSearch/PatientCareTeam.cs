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
    /// associated <see cref="CareTeam"/> resources in a single orchestrated call.
    /// </summary>
    public static partial class MultiResourceSearch
    {
        /// <summary>
        /// Retrieves a patient by identifier and returns the patient resource along with all
        /// associated <see cref="CareTeam"/> resources, including participant references.
        /// </summary>
        /// <param name="configurator">Client configurator providing resource clients and authentication.</param>
        /// <param name="patientIdentifier">Patient MRN or FHIR <c>system|value</c> identifier.</param>
        /// <param name="listReturnLimit">
        /// Defensive cap on the number of returned <see cref="CareTeam"/> resources.
        /// Values &lt;= 0 are treated as unbounded (<see cref="int.MaxValue"/>).
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A tuple of the resolved <see cref="Patient"/> (or <see langword="null"/> if not found)
        /// and the list of matching <see cref="CareTeam"/> resources.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configurator"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="patientIdentifier"/> is null or whitespace.</exception>
        public static async Task<(Patient? patient, List<CareTeam> careTeams)> PatientAndCareTeamAsync(
            ClientConfigurator configurator,
            string patientIdentifier,
            int listReturnLimit = -1,
            CancellationToken ct = default)
        {
            if (configurator is null) throw new ArgumentNullException(nameof(configurator));
            Ensure.NotNullOrWhiteSpace(patientIdentifier, nameof(patientIdentifier));

            if (listReturnLimit <= 0) listReturnLimit = int.MaxValue;

            var patient = await ResolvePatientAsync(configurator, patientIdentifier, ct).ConfigureAwait(false);
            if (patient is null)
                return (null, new List<CareTeam>());

            var patientId = patient.Id ?? throw new InvalidOperationException(
                $"Patient resolved for identifier '{PhiMask.Mask(patientIdentifier)}' has a null or empty Id.");

            var careTeamClient = configurator.ForResource<CareTeam>(ct);
            var b = new Builder<CareTeam>()
                        .ForPatient(patientId)
                        .Include("CareTeam:participant");
            if (listReturnLimit != int.MaxValue) b.WithCount(listReturnLimit);

            var careTeams = await careTeamClient.AggregateResourcesAsync(b.Build()).ConfigureAwait(false);
            return (patient, careTeams);
        }
    }
}
