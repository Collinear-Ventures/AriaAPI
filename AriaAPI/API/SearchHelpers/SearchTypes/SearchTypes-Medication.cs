// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
namespace AriaAPI.API.SearchHelpers
{
    public static partial class SearchTypes
    {
        /// <summary>
        /// FHIR R4 status values for the <c>MedicationRequest.status</c> search parameter.
        /// </summary>
        public enum MedicationRequestStatus
        {
            /// <summary>active</summary>
            Active,
            /// <summary>on-hold</summary>
            OnHold,
            /// <summary>cancelled</summary>
            Cancelled,
            /// <summary>completed</summary>
            Completed,
            /// <summary>entered-in-error</summary>
            EnteredInError,
            /// <summary>stopped</summary>
            Stopped,
            /// <summary>draft</summary>
            Draft,
            /// <summary>unknown</summary>
            Unknown
        }

        /// <summary>
        /// Maps <see cref="MedicationRequestStatus"/> to the token string expected by the FHIR server.
        /// </summary>
        public static string MedicationRequestStatusToToken(MedicationRequestStatus s) => s switch
        {
            MedicationRequestStatus.Active => "active",
            MedicationRequestStatus.OnHold => "on-hold",
            MedicationRequestStatus.Cancelled => "cancelled",
            MedicationRequestStatus.Completed => "completed",
            MedicationRequestStatus.EnteredInError => "entered-in-error",
            MedicationRequestStatus.Stopped => "stopped",
            MedicationRequestStatus.Draft => "draft",
            MedicationRequestStatus.Unknown => "unknown",
            _ => s.ToString().ToLowerInvariant()
        };

        /// <summary>
        /// FHIR R4 status values for the <c>MedicationAdministration.status</c> search parameter.
        /// </summary>
        public enum MedicationAdministrationStatus
        {
            /// <summary>in-progress</summary>
            InProgress,
            /// <summary>not-done</summary>
            NotDone,
            /// <summary>on-hold</summary>
            OnHold,
            /// <summary>completed</summary>
            Completed,
            /// <summary>entered-in-error</summary>
            EnteredInError,
            /// <summary>stopped</summary>
            Stopped,
            /// <summary>unknown</summary>
            Unknown
        }

        /// <summary>
        /// Maps <see cref="MedicationAdministrationStatus"/> to the token string expected by the FHIR server.
        /// </summary>
        public static string MedicationAdministrationStatusToToken(MedicationAdministrationStatus s) => s switch
        {
            MedicationAdministrationStatus.InProgress => "in-progress",
            MedicationAdministrationStatus.NotDone => "not-done",
            MedicationAdministrationStatus.OnHold => "on-hold",
            MedicationAdministrationStatus.Completed => "completed",
            MedicationAdministrationStatus.EnteredInError => "entered-in-error",
            MedicationAdministrationStatus.Stopped => "stopped",
            MedicationAdministrationStatus.Unknown => "unknown",
            _ => s.ToString().ToLowerInvariant()
        };
    }
}
