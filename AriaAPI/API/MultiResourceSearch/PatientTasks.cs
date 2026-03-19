// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.Core;
using AriaAPI.Networking.Core;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static AriaAPI.API.SearchHelpers.SearchTypes;

namespace AriaAPI.API.MultiResourceSearch
{
    public static partial class MultiResourceSearch
    {

        /// <summary>
        /// Retrieves a patient by identifier and returns the patient resource along with
        /// all associated <see cref="Hl7.Fhir.Model.Task"/> resources, optionally filtered by document type.
        /// </summary>
        /// <param name="configurator">Client configurator providing resource clients and authentication.</param>
        /// <param name="patientIdentifier">Patient identifier used to resolve the patient resource.</param>
        /// <param name="documentType">Optional document type filter token. If empty, all tasks are returned.</param>
        /// <param name="listReturnLimit">Defensive cap on the number of returned tasks. Values &lt;= 0 are treated as unbounded.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>
        /// A tuple of the resolved <see cref="Patient"/> (or <c>null</c> if not found) and the list of
        /// matching <see cref="Hl7.Fhir.Model.Task"/> resources.
        /// </returns>
        public static async Task<(Patient? patient, List<Hl7.Fhir.Model.Task> documents)>
            PatientWithTasksAsync(
                ClientConfigurator configurator,
                string patientIdentifier,
                string documentType = "",
                int listReturnLimit = -1,
                CancellationToken ct = default)
        {
            if (listReturnLimit <= 0) listReturnLimit = int.MaxValue;

            // 1) Search Patient
            var patientClient = configurator.ForResource<Patient>(ct);
            var patientParams = new Builder<Patient>().ByIdentifier(patientIdentifier).Build();
            var patientResults = await patientClient.SearchFirstPageAsync(patientParams, pageSize: 1)
                                                    .ConfigureAwait(false);
            var patient = patientResults.FirstOrDefault();

            if (patient == null)
                return (null, new List<Hl7.Fhir.Model.Task>());

            var patientId = patient.Id
                ?? throw new InvalidOperationException("Resolved Patient resource has a null or empty Id.");

            // 2) Search Task
            var taskClient = configurator.ForResource<Hl7.Fhir.Model.Task>(ct);
            SearchParams taskParams;

            if (string.IsNullOrWhiteSpace(documentType))
            {
                var b = new Builder<Hl7.Fhir.Model.Task>().ForPatient(patientId);
                if (listReturnLimit != int.MaxValue) b.WithCount(listReturnLimit);
                taskParams = b.Build();
            }
            else
            {
                var b = new Builder<Hl7.Fhir.Model.Task>()
                                .ForPatient(patientId)
                                .ByType(documentType);  // assumes your Builder sets the "type" token search parameter
                if (listReturnLimit != int.MaxValue) b.WithCount(listReturnLimit);
                taskParams = b.Build();
            }

            var tasks = await taskClient.AggregateResourcesAsync(taskParams).ConfigureAwait(false);
            return (patient, tasks);
        }

    }
}
