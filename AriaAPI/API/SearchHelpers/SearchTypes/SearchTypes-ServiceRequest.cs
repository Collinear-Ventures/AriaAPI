// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿namespace AriaAPI.API.SearchHelpers
{
    /// <summary>
    /// Provides strongly-typed enumerations and mapping dictionaries for FHIR search parameter values
    /// across multiple resource types (Appointment, Device, Location, Observation, etc.).
    /// </summary>
    public static partial class SearchTypes
    {
        /// <summary>
        /// Enumerates allowed ServiceRequest intents.
        /// </summary>
        public enum ServiceRequestIntentShort
        {
            /// <summary>The Original Order intent.</summary>
            OriginalOrder,
            /// <summary>The Filler Order intent.</summary>
            FillerOrder
        }

        /// <summary>
        /// Maps <see cref="ServiceRequestIntentShort"/> to the canonical FHIR token.
        /// </summary>
        public static string IntentToToken(ServiceRequestIntentShort s) => s switch
        {
            ServiceRequestIntentShort.OriginalOrder => "original-order",
            ServiceRequestIntentShort.FillerOrder => "filler-order",
            _ => s.ToString().ToLowerInvariant().Replace("_", "-")
        };
    }
}