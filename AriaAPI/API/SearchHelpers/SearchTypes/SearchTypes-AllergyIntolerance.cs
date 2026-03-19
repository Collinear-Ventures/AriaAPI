// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AriaAPI.API.SearchHelpers
{
    public static partial class SearchTypes
    {
        /// <summary>
        /// Enumerates AllergyIntolerance categories for the <c>category</c> search parameter.
        /// (FHIR values: food | medication)
        /// </summary>
        public enum AllergyCategory
        {
            /// <summary>Food allergies.</summary>
            Food,
            /// <summary>Medication/Drug allergies.</summary>
            Medication
        }

        /// <summary>
        /// Enumerates verification statuses for the <c>verification-status</c> search parameter.
        /// (FHIR values: confirmed | unconfirmed | entered-in-error)
        /// </summary>
        public enum AllergyVerificationStatus
        {
            /// <summary>Allergy is confirmed.</summary>
            Confirmed,
            /// <summary>Allergy is unconfirmed.</summary>
            Unconfirmed,
            /// <summary>Record was entered in error.</summary>
            EnteredInError
        }

        /// <summary>
        /// Maps <see cref="AllergyCategory"/> to FHIR search parameter value.
        /// </summary>
        public static string CategoryToParam(AllergyCategory cat) => cat switch
        {
            AllergyCategory.Food => "food",
            AllergyCategory.Medication => "medication",
            _ => cat.ToString().ToLowerInvariant()
        };

        /// <summary>
        /// Maps <see cref="AllergyVerificationStatus"/> to FHIR token value.
        /// </summary>
        public static string VerificationStatusToParam(AllergyVerificationStatus status) => status switch
        {
            AllergyVerificationStatus.Confirmed => "confirmed",
            AllergyVerificationStatus.Unconfirmed => "unconfirmed",
            AllergyVerificationStatus.EnteredInError => "entered-in-error",
            _ => status.ToString().ToLowerInvariant()
        };
    }
}
