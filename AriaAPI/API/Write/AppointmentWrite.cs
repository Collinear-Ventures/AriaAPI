// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.API.IdentityResolvers;
using AriaAPI.Core;
using AriaAPI.Networking.Core;
using AriaAPI.Networking.Helpers;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AriaAPI.API.Write
{
    /// <summary>
    /// Provides update and upsert operations for FHIR <see cref="Appointment"/> resources.
    /// </summary>
    /// <remarks>
    /// Patient-centric overloads resolve a patient by MRN or FHIR identifier and add a patient
    /// <see cref="Appointment.ParticipantComponent"/> if one is not already present.
    /// PHI is never logged in plain text — identifiers are masked via <see cref="PhiMask"/>.
    /// </remarks>
    public static class AppointmentWrite
    {
        // ── Shape 1: resource-centric ──────────────────────────────────────────

        /// <summary>
        /// Updates an existing <see cref="Appointment"/> resource on the FHIR server using its logical ID.
        /// </summary>
        /// <param name="configurator">FHIR client configurator.</param>
        /// <param name="resource">
        /// The fully constructed <see cref="Appointment"/> to update.
        /// <see cref="Resource.Id"/> must be set to the existing server-assigned logical ID.
        /// </param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="versionAware">
        /// When <see langword="true"/> (default), sends an <c>If-Match</c> ETag header.
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The updated <see cref="Appointment"/> as returned by the server.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configurator"/> or <paramref name="resource"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="resource"/>.Id is null or whitespace.</exception>
        public static async Task<Appointment> UpdateAsync(
            ClientConfigurator configurator,
            Appointment resource,
            ILogger logger,
            bool versionAware = true,
            CancellationToken ct = default)
        {
            if (configurator is null) throw new ArgumentNullException(nameof(configurator));
            if (resource is null) throw new ArgumentNullException(nameof(resource));
            if (string.IsNullOrWhiteSpace(resource.Id))
                throw new ArgumentException(
                    "resource.Id must be set for an update operation (PUT requires a logical ID).",
                    nameof(resource));

            var client = configurator.ForResource<Appointment>(ct);
            var updated = await client.UpdateAsync(resource, versionAware).ConfigureAwait(false);

            if (updated is null)
                throw new InvalidOperationException(
                    $"FHIR server returned null for Appointment update (id: {PhiMask.Mask(resource.Id)}).");
            logger.LogInformation("Appointment updated with id: {Id}", PhiMask.Mask(updated.Id));
            return updated;
        }

        /// <summary>
        /// Conditionally creates or updates an <see cref="Appointment"/> resource using a search identifier
        /// (PUT with search criteria — creates if no match, updates if exactly one match).
        /// </summary>
        /// <param name="configurator">FHIR client configurator.</param>
        /// <param name="resource">The fully constructed <see cref="Appointment"/> to upsert.</param>
        /// <param name="identifier">
        /// Conditional PUT search criteria as a raw query string, e.g. <c>"identifier=urn:aria:appt|appt-001"</c>.
        /// Must match zero or one existing resources; the server returns 412 if multiple match.
        /// </param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The created or updated <see cref="Appointment"/> as returned by the server.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configurator"/> or <paramref name="resource"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="identifier"/> is null or whitespace.</exception>
        public static async Task<Appointment> UpsertAsync(
            ClientConfigurator configurator,
            Appointment resource,
            string identifier,
            ILogger logger,
            CancellationToken ct = default)
        {
            if (configurator is null) throw new ArgumentNullException(nameof(configurator));
            if (resource is null) throw new ArgumentNullException(nameof(resource));
            Ensure.NotNullOrWhiteSpace(identifier, nameof(identifier));

            if (!identifier.Contains('='))
                throw new ArgumentException(
                    "identifier must be in 'param=value' format (e.g. \"identifier=urn:aria:appt|appt-001\").",
                    nameof(identifier));

            var eqIndex = identifier.IndexOf('=');
            var paramName = eqIndex >= 0 ? identifier.Substring(0, eqIndex) : identifier;
            var paramValue = eqIndex >= 0 ? identifier.Substring(eqIndex + 1) : string.Empty;

            var condition = SearchParams.FromUriParamList(
                new List<Tuple<string, string>> { Tuple.Create(paramName, paramValue) });

            var client = configurator.ForResource<Appointment>(ct);
            var upserted = await client.ConditionalUpdateAsync(resource, condition).ConfigureAwait(false);

            if (upserted is null)
                throw new InvalidOperationException(
                    $"FHIR server returned null for Appointment upsert (identifier: {PhiMask.Mask(resource.Id)}).");
            logger.LogInformation("Appointment upserted with id: {Id}", PhiMask.Mask(upserted.Id));
            return upserted;
        }

        // ── Shape 2: patient-centric ───────────────────────────────────────────

        /// <summary>
        /// Resolves a patient by MRN or FHIR identifier, adds a patient participant to the
        /// <see cref="Appointment"/> if none is present, then updates the resource using its logical ID.
        /// </summary>
        /// <param name="configurator">FHIR client configurator.</param>
        /// <param name="patientIdentifier">Patient MRN or FHIR <c>system|value</c> identifier.</param>
        /// <param name="resource">
        /// The fully constructed <see cref="Appointment"/> to update.
        /// <see cref="Resource.Id"/> must be set.
        /// </param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="versionAware">
        /// When <see langword="true"/> (default), sends an <c>If-Match</c> ETag header.
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The updated <see cref="Appointment"/> as returned by the server.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configurator"/> or <paramref name="resource"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="patientIdentifier"/> is null or whitespace.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no patient is found for <paramref name="patientIdentifier"/>.</exception>
        public static async Task<Appointment> UpdateForPatientAsync(
            ClientConfigurator configurator,
            string patientIdentifier,
            Appointment resource,
            ILogger logger,
            bool versionAware = true,
            CancellationToken ct = default)
        {
            if (configurator is null) throw new ArgumentNullException(nameof(configurator));
            Ensure.NotNullOrWhiteSpace(patientIdentifier, nameof(patientIdentifier));
            if (resource is null) throw new ArgumentNullException(nameof(resource));

            var patient = await ResolvePatientAsync(configurator, patientIdentifier, ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException(
                    $"Patient not found for identifier '{PhiMask.Mask(patientIdentifier)}'.");
            EnsurePatientParticipant(resource, patient.Id!);

            return await UpdateAsync(configurator, resource, logger, versionAware, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Resolves a patient by MRN or FHIR identifier, adds a patient participant to the
        /// <see cref="Appointment"/> if none is present, then conditionally creates or updates the resource.
        /// </summary>
        /// <param name="configurator">FHIR client configurator.</param>
        /// <param name="patientIdentifier">Patient MRN or FHIR <c>system|value</c> identifier.</param>
        /// <param name="resource">The fully constructed <see cref="Appointment"/> to upsert.</param>
        /// <param name="identifier">Conditional PUT search criteria, e.g. <c>"identifier=urn:aria:appt|appt-001"</c>.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The created or updated <see cref="Appointment"/> as returned by the server.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configurator"/> or <paramref name="resource"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="patientIdentifier"/> or <paramref name="identifier"/> is null or whitespace.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no patient is found for <paramref name="patientIdentifier"/>.</exception>
        public static async Task<Appointment> UpsertForPatientAsync(
            ClientConfigurator configurator,
            string patientIdentifier,
            Appointment resource,
            string identifier,
            ILogger logger,
            CancellationToken ct = default)
        {
            if (configurator is null) throw new ArgumentNullException(nameof(configurator));
            Ensure.NotNullOrWhiteSpace(patientIdentifier, nameof(patientIdentifier));
            if (resource is null) throw new ArgumentNullException(nameof(resource));
            Ensure.NotNullOrWhiteSpace(identifier, nameof(identifier));

            var patient = await ResolvePatientAsync(configurator, patientIdentifier, ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException(
                    $"Patient not found for identifier '{PhiMask.Mask(patientIdentifier)}'.");
            EnsurePatientParticipant(resource, patient.Id!);

            return await UpsertAsync(configurator, resource, identifier, logger, ct).ConfigureAwait(false);
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private static async Task<Patient?> ResolvePatientAsync(
            ClientConfigurator configurator,
            string patientIdentifier,
            CancellationToken ct)
        {
            var patientClient = configurator.ForResource<Patient>(ct);
            var patientParams = new Builder<Patient>().ByIdentifier(patientIdentifier).Build();
            var results = await patientClient.SearchFirstPageAsync(patientParams, pageSize: 1).ConfigureAwait(false);
            var patient = results.FirstOrDefault();

            if (patient is not null && string.IsNullOrWhiteSpace(patient.Id))
                throw new InvalidOperationException(
                    $"Patient resolved for identifier '{PhiMask.Mask(patientIdentifier)}' has a null or empty Id.");

            return patient;
        }

        /// <summary>
        /// Adds a patient participant to the appointment if no patient participant already exists.
        /// </summary>
        private static void EnsurePatientParticipant(Appointment resource, string patientId)
        {
            var alreadyPresent = resource.Participant?.Any(
                p => p.Actor?.Reference?.StartsWith("Patient/", StringComparison.OrdinalIgnoreCase) == true) == true;

            if (!alreadyPresent)
            {
                resource.Participant ??= new List<Appointment.ParticipantComponent>();
                resource.Participant.Add(new Appointment.ParticipantComponent
                {
                    Actor = new ResourceReference($"Patient/{patientId}"),
                    Status = ParticipationStatus.Accepted
                });
            }
        }
    }
}
