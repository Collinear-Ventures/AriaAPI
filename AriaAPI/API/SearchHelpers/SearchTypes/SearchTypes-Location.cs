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
        /// Enumerates allowed statuses for <c>status</c>.
        /// </summary>
        public enum LocationStatus
        {
            /// <summary>The Active status.</summary>
            Active,
            /// <summary>The Inactive status.</summary>
            Inactive
        }

        /// <summary>
        /// Enumerates allowed types for <c>type</c>.
        /// </summary>
        public enum LocationType
        {
            /// <summary>The Auxiliary type.</summary>
            Auxiliary,
            /// <summary>The Venue type.</summary>
            Venue
        }

        /// <summary>
        /// Converts a <see cref="LocationStatus"/> value to the FHIR token string.
        /// </summary>
        public static string StatusToToken(LocationStatus status) => status switch
        {
            LocationStatus.Active => "active",
            LocationStatus.Inactive => "inactive",
            _ => status.ToString().ToLowerInvariant()
        };

        /// <summary>
        /// Converts a <see cref="LocationType"/> value to the FHIR token string.
        /// </summary>
        public static string TypeToToken(LocationType type) => type switch
        {
            LocationType.Auxiliary => "auxiliary",
            LocationType.Venue => "venue",
            _ => type.ToString().ToLowerInvariant()
        };
    }
}
