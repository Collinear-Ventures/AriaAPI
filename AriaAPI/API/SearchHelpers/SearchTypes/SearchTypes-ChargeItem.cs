// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AriaAPI.API.SearchHelpers
{
    /// <summary>
    /// Provides strongly-typed enumerations and mapping dictionaries for FHIR search parameter values
    /// across multiple resource types (Appointment, Device, Location, Observation, etc.).
    /// </summary>
    public static partial class SearchTypes
    {
        /// <summary>
        /// Enumerates allowed charge categories for the <c>category</c> search parameter.
        /// </summary>
        public enum ChargeCategory
        {
            /// <summary>The Technical charge category.</summary>
            Technical,
            /// <summary>The Administrative charge category.</summary>
            Administrative,
            /// <summary>The Global charge category.</summary>
            Global,
            /// <summary>The Medication Oncology charge category.</summary>
            MedicationOncology,
            /// <summary>The Professional charge category.</summary>
            Professional
        }

        /// <summary>
        /// Enumerates allowed statuses for the <c>status</c> search parameter.
        /// </summary>
        public enum ChargeStatus
        {
            /// <summary>The Planned status.</summary>
            Planned,
            /// <summary>The Billable status.</summary>
            Billable,
            /// <summary>The Not Billable status.</summary>
            NotBillable,
            /// <summary>The Billed status.</summary>
            Billed,
            /// <summary>The Entered In Error status.</summary>
            EnteredInError
        }

        /// <summary>
        /// Converts a <see cref="ChargeCategory"/> value to the FHIR search parameter string.
        /// </summary>
        public static string CategoryToParam(ChargeCategory cat) => cat switch
        {
            ChargeCategory.Technical => "Technical",
            ChargeCategory.Administrative => "Administrative",
            ChargeCategory.Global => "Global",
            ChargeCategory.MedicationOncology => "Medication Oncology",
            ChargeCategory.Professional => "Professional",
            _ => cat.ToString()
        };

        /// <summary>
        /// Converts a <see cref="ChargeStatus"/> value to the FHIR search parameter string.
        /// </summary>
        public static string StatusToParam(ChargeStatus status) => status switch
        {
            ChargeStatus.Planned => "planned",
            ChargeStatus.Billable => "billable",
            ChargeStatus.NotBillable => "not-billable",
            ChargeStatus.Billed => "billed",
            ChargeStatus.EnteredInError => "entered-in-error",
            _ => status.ToString().ToLowerInvariant()
        };
    }
}
