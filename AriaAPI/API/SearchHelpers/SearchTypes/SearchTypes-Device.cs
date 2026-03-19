// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AriaAPI.API.SingleResourceSearch.DeviceSearch;

namespace AriaAPI.API.SearchHelpers
{
    /// <summary>
    /// Provides strongly-typed enumerations and mapping dictionaries for FHIR search parameter values
    /// across multiple resource types (Appointment, Device, Location, Observation, etc.).
    /// </summary>
    public static partial class SearchTypes
    {
        /// <summary>
        /// Controls how the search handles servers that reject <c>device-name</c> + <c>_include</c>.
        /// </summary>
        public enum IncludeConflictPolicy
        {
            /// <summary>
            /// If device-name is specified, automatically suppress includes in the initial search.
            /// </summary>
            SuppressIncludes,

            /// <summary>
            /// Two-phase enrichment: first search by device-name without includes; then fetch includes
            /// via follow-up requests (by id) to rehydrate related resources.
            /// </summary>
            EnrichInSecondPass
        }

        /// <summary>
        /// Enumerates allowed statuses for the <c>status</c> search parameter.
        /// </summary>
        public enum DeviceStatus
        {
            /// <summary>The Active status.</summary>
            Active,
            /// <summary>The Inactive status.</summary>
            Inactive
        }

        /// <summary>
        /// Converts a <see cref="DeviceStatus"/> value to the FHIR token string.
        /// </summary>
        public static string StatusToToken(DeviceStatus status) => status switch
        {
            DeviceStatus.Active => "active",
            DeviceStatus.Inactive => "inactive",
            _ => status.ToString().ToLowerInvariant()
        };
    }
}
