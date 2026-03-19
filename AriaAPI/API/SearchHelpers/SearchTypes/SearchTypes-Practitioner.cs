// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using System;

namespace AriaAPI.API.SearchHelpers
{
    /// <summary>
    /// Partial class containing search type enumerations and mappings for FHIR resource searches.
    /// </summary>
    public static partial class SearchTypes
    {
        /// <summary>
        /// Enumerates common practitioner roles for tokenized search.
        /// Extend/align to your server’s code system as needed.
        /// </summary>
        public enum PractitionerRoleShort
        {
            /// <summary>The physician role.</summary>
            Physician,
            /// <summary>The surgeon role.</summary>
            Surgeon,
            /// <summary>The oncologist role.</summary>
            Oncologist,
            /// <summary>The radiologist role.</summary>
            Radiologist,
            /// <summary>The nurse role.</summary>
            Nurse,
            /// <summary>The nurse practitioner role.</summary>
            NursePractitioner,
            /// <summary>The pharmacist role.</summary>
            Pharmacist,
            /// <summary>The dentist role.</summary>
            Dentist,
            /// <summary>The therapist role.</summary>
            Therapist,
            /// <summary>The physical therapist role.</summary>
            PhysicalTherapist,
            /// <summary>The occupational therapist role.</summary>
            OccupationalTherapist,
            /// <summary>The speech therapist role.</summary>
            SpeechTherapist,
            /// <summary>The technician role.</summary>
            Technician,
            /// <summary>The lab technician role.</summary>
            LabTechnician,
            /// <summary>The social worker role.</summary>
            SocialWorker,
            /// <summary>The administrator role.</summary>
            Administrator,
            /// <summary>The midwife role.</summary>
            Midwife,
            /// <summary>The researcher role.</summary>
            Researcher,
            /// <summary>The optometrist role.</summary>
            Optometrist,
            /// <summary>The podiatrist role.</summary>
            Podiatrist,
            /// <summary>The counselor role.</summary>
            Counselor,
            /// <summary>The psychologist role.</summary>
            Psychologist,
            /// <summary>Use Other when providing a custom token via string.</summary>
            Other
        }

        /// <summary>
        /// Converts <see cref="PractitionerRoleShort"/> to a canonical token
        /// suitable for the FHIR <c>practitioner-role</c> search parameter.
        /// Tokens use lower-kebab-case to align with common conventions.
        /// </summary>
        public static string RoleToToken(PractitionerRoleShort r) => r switch
        {
            PractitionerRoleShort.Physician => "physician",
            PractitionerRoleShort.Surgeon => "surgeon",
            PractitionerRoleShort.Oncologist => "oncologist",
            PractitionerRoleShort.Radiologist => "radiologist",
            PractitionerRoleShort.Nurse => "nurse",
            PractitionerRoleShort.NursePractitioner => "nurse-practitioner",
            PractitionerRoleShort.Pharmacist => "pharmacist",
            PractitionerRoleShort.Dentist => "dentist",
            PractitionerRoleShort.Therapist => "therapist",
            PractitionerRoleShort.PhysicalTherapist => "physical-therapist",
            PractitionerRoleShort.OccupationalTherapist => "occupational-therapist",
            PractitionerRoleShort.SpeechTherapist => "speech-therapist",
            PractitionerRoleShort.Technician => "technician",
            PractitionerRoleShort.LabTechnician => "lab-technician",
            PractitionerRoleShort.SocialWorker => "social-worker",
            PractitionerRoleShort.Administrator => "administrator",
            PractitionerRoleShort.Midwife => "midwife",
            PractitionerRoleShort.Researcher => "researcher",
            PractitionerRoleShort.Optometrist => "optometrist",
            PractitionerRoleShort.Podiatrist => "podiatrist",
            PractitionerRoleShort.Counselor => "counselor",
            PractitionerRoleShort.Psychologist => "psychologist",
            PractitionerRoleShort.Other => "other",
            _ => r.ToString().ToLowerInvariant().Replace("_", "-")
        };
    }
}
