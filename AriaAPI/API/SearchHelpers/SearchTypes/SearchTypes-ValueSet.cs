// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AriaAPI.API.SearchHelpers
{
    /// <summary>
    /// Partial class containing search type enumerations and mappings for FHIR resource searches.
    /// </summary>
    public static partial class SearchTypes
    {
        /// <summary>
        /// Enumerates the ARIA-specific ValueSets commonly used across deployments.
        /// </summary>
        public enum AriaValueSet
        {
            /// <summary>The allergy intolerance category value set.</summary>
            AllergyIntoleranceCategory,
            /// <summary>The care plan template value set.</summary>
            CarePlanTemplate,
            /// <summary>The country value set.</summary>
            Country,
            /// <summary>The device type value set.</summary>
            DeviceType,
            /// <summary>The diagnosis code value set (supports context filter).</summary>
            DiagnosisCode,
            /// <summary>The document reference type value set (often requires publisher).</summary>
            DocumentReferenceType,
            /// <summary>The mobile phone provider value set.</summary>
            MobilePhoneProvider,
            /// <summary>The patient citizenship value set.</summary>
            PatientCitizenship,
            /// <summary>The patient death reason value set.</summary>
            PatientDeathReason,
            /// <summary>The patient ethnicity value set.</summary>
            PatientEthnicity,
            /// <summary>The patient gender value set.</summary>
            PatientGender,
            /// <summary>The patient gender identity value set.</summary>
            PatientGenderIdentity,
            /// <summary>The patient IDs value set.</summary>
            PatientIds,
            /// <summary>The patient language value set.</summary>
            PatientLanguage,
            /// <summary>The patient marital status value set.</summary>
            PatientMaritalStatus,
            /// <summary>The patient race value set.</summary>
            PatientRace,
            /// <summary>The patient religion value set.</summary>
            PatientReligion,
            /// <summary>The patient sexual orientation value set.</summary>
            PatientSexualOrientation,
            /// <summary>The patient user-defined label value set.</summary>
            PatientUserDefinedLabel,
            /// <summary>The patient status value set.</summary>
            PatientStatus,
            /// <summary>The practitioner IDs value set.</summary>
            PractitionerIds,
            /// <summary>The practitioner role value set.</summary>
            PractitionerRole
        }

        /// <summary>
        /// Maps ARIA logical names to canonical URLs used for ValueSet expansion.
        /// </summary>
        public static readonly Dictionary<AriaValueSet, string> AriaCanonicalMap = new()
        {
            { AriaValueSet.AllergyIntoleranceCategory, "http://varian.com/fhir/ValueSet/allergy-intolerance-category" },
            { AriaValueSet.CarePlanTemplate,           "http://varian.com/fhir/ValueSet/careplan-template" },
            { AriaValueSet.Country,                    "http://varian.com/fhir/ValueSet/country" },
            { AriaValueSet.DeviceType,                 "http://varian.com/fhir/ValueSet/device-type" },
            { AriaValueSet.DiagnosisCode,              "http://varian.com/fhir/ValueSet/condition-code" },
            { AriaValueSet.DocumentReferenceType,      "http://varian.com/fhir/ValueSet/documentreference-type" },
            { AriaValueSet.MobilePhoneProvider,        "http://varian.com/fhir/ValueSet/patient-mobilephoneprovider" },
            { AriaValueSet.PatientCitizenship,         "http://varian.com/fhir/ValueSet/country" },
            { AriaValueSet.PatientDeathReason,         "http://varian.com/fhir/ValueSet/patient-deathreason" },
            { AriaValueSet.PatientEthnicity,           "http://varian.com/fhir/ValueSet/ethnicity" },
            { AriaValueSet.PatientGender,              "http://varian.com/fhir/ValueSet/person-gender" },
            { AriaValueSet.PatientGenderIdentity,      "http://varian.com/fhir/ValueSet/patient-genderIdentity" },
            { AriaValueSet.PatientIds,                 "http://varian.com/fhir/ValueSet/patient-ids" },
            { AriaValueSet.PatientLanguage,            "http://varian.com/fhir/ValueSet/languages" },
            { AriaValueSet.PatientMaritalStatus,       "http://varian.com/fhir/ValueSet/marital-status" },
            { AriaValueSet.PatientRace,                "http://varian.com/fhir/ValueSet/race" },
            { AriaValueSet.PatientReligion,            "http://varian.com/fhir/ValueSet/religion" },
            { AriaValueSet.PatientSexualOrientation,   "http://varian.com/fhir/ValueSet/patient-sexorientation" },
            { AriaValueSet.PatientUserDefinedLabel,    "http://varian.com/fhir/ValueSet/patient-userdefinedlabel" },
            { AriaValueSet.PatientStatus,              "http://varian.com/fhir/ValueSet/patient-status" },
            { AriaValueSet.PractitionerIds,            "http://varian.com/fhir/ValueSet/practitioner-ids" },
            { AriaValueSet.PractitionerRole,           "http://varian.com/fhir/ValueSet/practitioner-role" }
        };

    }
}
