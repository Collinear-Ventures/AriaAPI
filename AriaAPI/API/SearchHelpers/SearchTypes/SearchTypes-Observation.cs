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
        /// Enumerates allowed statuses (limited set requested).
        /// </summary>
        public enum ObservationStatusShort
        {
            /// <summary>The Preliminary status.</summary>
            Preliminary,
            /// <summary>The Final status.</summary>
            Final,
            /// <summary>The Entered In Error status.</summary>
            EnteredInError
        }

        /// <summary>
        /// Converts an <see cref="ObservationStatusShort"/> value to the FHIR token string.
        /// </summary>
        public static string StatusToToken(ObservationStatusShort s) => s switch
        {
            ObservationStatusShort.Preliminary => "preliminary",
            ObservationStatusShort.Final => "final",
            ObservationStatusShort.EnteredInError => "entered-in-error",
            _ => s.ToString().ToLowerInvariant()
        };
    }
}
