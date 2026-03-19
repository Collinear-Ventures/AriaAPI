// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.Core;
using AriaAPI.Networking.Core;
using AriaAPI.Resources.Includes;
using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static AriaAPI.API.SearchHelpers.SearchTypes;

namespace AriaAPI.API.SingleResourceSearch
{
    public static partial class AppointmentSearch
    {
        /// <summary>
        /// Patient-first search using the vendor 'patient' parameter (if the server supports it).
        /// </summary>
        /// <param name="configurator">Client configurator to create the Appointment client.</param>
        /// <param name="patientIdOrRef">Patient id or reference (e.g., "123" or "Patient/123").</param>
        /// <param name="start">Optional start of date window.</param>
        /// <param name="end">Optional end of date window.</param>
        /// <param name="status">Optional appointment status to filter.</param>
        /// <param name="limit">Maximum appointments to return; -1 means no explicit limit.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Appointment>> SearchAppointmentsForPatientAsync(
            ClientConfigurator configurator,
            string patientIdOrRef,
            DateTimeOffset? start = null,
            DateTimeOffset? end = null,
            Appointment.AppointmentStatus? status = null,
            int limit = -1,
            CancellationToken ct = default)
        {
            var p = new AppointmentSearchParams
            {
                Patients = new List<string> { NormalizePatientRef(patientIdOrRef) },
                Start = start,
                End = end,
                Status = status,
                ListReturnLimit = limit
            };

            return SearchAppointmentsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Standards-friendly fallback: search by patient as an Appointment 'actor' (Patient/{id}).
        /// Use this if the FHIR server rejects the vendor 'patient' search parameter.
        /// </summary>
        /// <param name="configurator">Client configurator to create the Appointment client.</param>
        /// <param name="patientIdOrRef">Patient id or reference (e.g., "123" or "Patient/123").</param>
        /// <param name="start">Optional start of date window.</param>
        /// <param name="end">Optional end of date window.</param>
        /// <param name="status">Optional appointment status to filter.</param>
        /// <param name="limit">Maximum appointments to return; -1 means no explicit limit.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Appointment>> ForPatientAsActorAsync(
            ClientConfigurator configurator,
            string patientIdOrRef,
            DateTimeOffset? start = null,
            DateTimeOffset? end = null,
            Appointment.AppointmentStatus? status = null,
            int limit = -1,
            CancellationToken ct = default)
        {
            var p = new AppointmentSearchParams
            {
                Actors = new List<string> { NormalizePatientRef(patientIdOrRef) },
                Start = start,
                End = end,
                Status = status,
                ListReturnLimit = limit
            };

            return SearchAppointmentsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Convenience overload to search appointments for multiple patients in one call.
        /// Each patient identifier will be normalized to a reference if necessary.
        /// </summary>
        /// <param name="configurator">Client configurator to create the Appointment client.</param>
        /// <param name="patientIdsOrRefs">Collection of patient ids or references.</param>
        /// <param name="start">Optional start of date window.</param>
        /// <param name="end">Optional end of date window.</param>
        /// <param name="status">Optional appointment status to filter.</param>
        /// <param name="limit">Maximum appointments to return; -1 means no explicit limit.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Appointment>> ForPatientsAsync(
            ClientConfigurator configurator,
            IEnumerable<string> patientIdsOrRefs,
            DateTimeOffset? start = null,
            DateTimeOffset? end = null,
            Appointment.AppointmentStatus? status = null,
            int limit = -1,
            CancellationToken ct = default)
        {
            var pats = patientIdsOrRefs?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(NormalizePatientRef)
                .ToList() ?? new List<string>();

            var p = new AppointmentSearchParams
            {
                Patients = pats,
                Start = start,
                End = end,
                Status = status,
                ListReturnLimit = limit
            };

            return SearchAppointmentsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Convenience: search by patient + service-category + date window.
        /// </summary>
        /// <param name="configurator">Client configurator to create the Appointment client.</param>
        /// <param name="patientIdOrRef">Patient id or reference (e.g., "123" or "Patient/123").</param>
        /// <param name="serviceCategories">Service categories to filter appointments by.</param>
        /// <param name="start">Optional start of date window.</param>
        /// <param name="end">Optional end of date window.</param>
        /// <param name="status">Optional appointment status to filter.</param>
        /// <param name="limit">Maximum appointments to return; -1 means no explicit limit.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Appointment>> ForPatientByServiceCategoryAsync(
            ClientConfigurator configurator,
            string patientIdOrRef,
            IEnumerable<AppointmentCategory> serviceCategories,
            DateTimeOffset? start = null,
            DateTimeOffset? end = null,
            Appointment.AppointmentStatus? status = null,
            int limit = -1,
            CancellationToken ct = default)
        {
            var p = new AppointmentSearchParams
            {
                Patients = new List<string> { NormalizePatientRef(patientIdOrRef) },
                ServiceCategories = serviceCategories?.ToList() ?? [],
                Start = start,
                End = end,
                Status = status,
                ListReturnLimit = limit
            };

            return SearchAppointmentsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Return appointments within a date window that match one or more service categories and optional includes.
        /// </summary>
        /// <param name="configurator">Client configurator to create the Appointment client.</param>
        /// <param name="start">Start of date window.</param>
        /// <param name="end">End of date window.</param>
        /// <param name="serviceCategories">Service categories to filter appointments by.</param>
        /// <param name="includes">Optional includes to request with the search.</param>
        /// <param name="listReturnLimit">Maximum appointments to return; -1 means no explicit limit.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Appointment>> ByDateAndCategoryAsync(
            ClientConfigurator configurator,
            DateTimeOffset start,
            DateTimeOffset end,
            IEnumerable<AppointmentCategory> serviceCategories,
            IEnumerable<AppointmentInclude>? includes,
            int listReturnLimit = -1,
            bool useIterateModifier = true,
            CancellationToken ct = default)
        {
            // Normalize -1 to "no explicit limit"
            var limit = listReturnLimit <= 0 ? int.MaxValue : listReturnLimit;

            var p = new AppointmentSearchParams
            {
                Start = start,
                End = end,
                ServiceCategories = serviceCategories?.ToList() ?? [],
                ListReturnLimit = limit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };

            return SearchAppointmentsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Return appointments within a date window matching a specific set of service categories and status,
        /// with optional includes and iterate modifier usage.
        /// </summary>
        /// <param name="configurator">Client configurator to create the Appointment client.</param>
        /// <param name="start">Start of date window.</param>
        /// <param name="end">End of date window.</param>
        /// <param name="serviceCategories">Service categories to filter appointments by.</param>
        /// <param name="includes">Optional includes to request with the search.</param>
        /// <param name="status">Optional appointment status to filter.</param>
        /// <param name="listReturnLimit">Maximum appointments to return; -1 means no explicit limit.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Appointment>> ByDateCategoryStatusAsync(
            ClientConfigurator configurator,
            DateTimeOffset start,
            DateTimeOffset end,
            IEnumerable<AppointmentCategory> serviceCategories,
            IEnumerable<AppointmentInclude>? includes,
            Appointment.AppointmentStatus? status,
            int listReturnLimit = -1,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            // Normalize -1 to "no explicit limit"
            var limit = listReturnLimit <= 0 ? int.MaxValue : listReturnLimit;

            var p = new AppointmentSearchParams
            {
                Start = start,
                End = end,
                ServiceCategories = serviceCategories?.ToList() ?? [],
                ListReturnLimit = limit,
                Includes = includes,
                Status = status,
                UseIterateModifier = useIterateModifier
            };

            return SearchAppointmentsAsync(configurator, p, ct);
        }
    }
}
