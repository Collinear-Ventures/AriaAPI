// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using System;

namespace AriaAPI.API.SearchHelpers
{
    public static partial class SearchTypes
    {
        /// <summary>
        /// FHIR R4 status values for the <c>Encounter.status</c> search parameter.
        /// </summary>
        public enum EncounterStatus
        {
            /// <summary>planned</summary>
            Planned,
            /// <summary>arrived</summary>
            Arrived,
            /// <summary>triaged</summary>
            Triaged,
            /// <summary>in-progress</summary>
            InProgress,
            /// <summary>onleave</summary>
            OnLeave,
            /// <summary>finished</summary>
            Finished,
            /// <summary>cancelled</summary>
            Cancelled,
            /// <summary>entered-in-error</summary>
            EnteredInError,
            /// <summary>unknown</summary>
            Unknown
        }

        /// <summary>
        /// Maps <see cref="EncounterStatus"/> to the token string expected by the FHIR server.
        /// </summary>
        public static string EncounterStatusToToken(EncounterStatus status) => status switch
        {
            EncounterStatus.Planned => "planned",
            EncounterStatus.Arrived => "arrived",
            EncounterStatus.Triaged => "triaged",
            EncounterStatus.InProgress => "in-progress",
            EncounterStatus.OnLeave => "onleave",
            EncounterStatus.Finished => "finished",
            EncounterStatus.Cancelled => "cancelled",
            EncounterStatus.EnteredInError => "entered-in-error",
            EncounterStatus.Unknown => "unknown",
            _ => status.ToString().ToLowerInvariant()
        };
    }
}
