// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using AriaAPI.Core;
using AriaAPI.Networking.Core;
using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AriaAPI.API.MultiResourceSearch
{
    public static partial class MultiResourceSearch
    {

        /// <summary>
        /// Searches for a patient by identifier, then retrieves all associated <see cref="DocumentReference"/> and <see cref="Appointment"/> resources.
        /// </summary>
        /// <param name="configurator">
        /// The <see cref="ClientConfigurator"/> instance used to create FHIR resource clients and manage authentication.
        /// </param>
        /// <param name="patientIdentifier">
        /// The patient identifier (e.g., MRN or business identifier) used to locate the patient resource.
        /// </param>
        /// <param name="listReturnLimit">
        /// The maximum number of <see cref="DocumentReference"/> and <see cref="Appointment"/> resources to return for the patient.
        /// If set to zero or a negative value, all available resources are returned.
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A tuple containing:
        /// <list type="bullet">
        ///   <item><description>The matched <see cref="Patient"/> resource, or <c>null</c> if not found.</description></item>
        ///   <item><description>A <see cref="List{DocumentReference}"/> of documents associated with the patient.</description></item>
        ///   <item><description>A <see cref="List{Appointment}"/> of appointments associated with the patient.</description></item>
        /// </list>
        /// If no patient is found, both lists will be empty.
        /// </returns>
        /// <remarks>
        /// This method performs three FHIR searches:
        /// <list type="number">
        ///   <item>Searches for the patient using the provided identifier.</item>
        ///   <item>If found, searches for all <see cref="DocumentReference"/> resources linked to the patient.</item>
        ///   <item>Searches for all <see cref="Appointment"/> resources linked to the patient.</item>
        /// </list>
        /// All searches use the shared <see cref="ClientConfigurator"/> for efficient connection reuse and authentication.
        /// </remarks>

        public static async Task<(Patient? patient, List<DocumentReference> documents, List<Appointment> appointments)>
            PatientWithDocumentsAndAppointmentsAsync(ClientConfigurator configurator, string patientIdentifier, int listReturnLimit = -1, CancellationToken ct = default)
        {
            if (listReturnLimit <= 0)
            {
                listReturnLimit = int.MaxValue;
            }

            // 1. Search for Patient
            var patientClient = configurator.ForResource<Patient>(ct);
            var patientParams = new Builder<Patient>().ByIdentifier(patientIdentifier).Build();
            var patientResults = await patientClient.SearchFirstPageAsync(patientParams, pageSize: 1)
                                                   .ConfigureAwait(false);
            var patient = patientResults.FirstOrDefault();

            if (patient == null)
                return (null, new List<DocumentReference>(), new List<Appointment>());

            var patientId = patient.Id
                ?? throw new InvalidOperationException("Resolved Patient resource has a null or empty Id.");

            // 2. Search for DocumentReference
            var docClient = configurator.ForResource<DocumentReference>(ct);
            var docBuilder = new Builder<DocumentReference>().ForPatient(patientId);
            if (listReturnLimit != int.MaxValue) docBuilder.WithCount(listReturnLimit);
            var documents = await docClient.AggregateResourcesAsync(docBuilder.Build()).ConfigureAwait(false);

            // 3. Search for Appointment
            var apptClient = configurator.ForResource<Appointment>(ct);
            var apptBuilder = new Builder<Appointment>().ForPatient(patientId);
            if (listReturnLimit != int.MaxValue) apptBuilder.WithCount(listReturnLimit);
            var appointments = await apptClient.AggregateResourcesAsync(apptBuilder.Build()).ConfigureAwait(false);

            return (patient, documents, appointments);
        }
    }
}
