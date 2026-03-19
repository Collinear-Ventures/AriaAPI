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
        /// Enumerates allowed values for the <c>code</c> search parameter in Group.
        /// </summary>
        public enum GroupCode
        {
            /// <summary>The Staff code.</summary>
            Staff,
            /// <summary>The Resource code.</summary>
            Resource,
            /// <summary>The Resource And Staff code.</summary>
            ResourceAndStaff
        }

        /// <summary>
        /// Converts GroupCode enum to FHIR token string.
        /// </summary>
        public static string GroupCodeToToken(GroupCode code) => code switch
        {
            GroupCode.Staff => "Staff",
            GroupCode.Resource => "Resource",
            GroupCode.ResourceAndStaff => "ResourceAndStaff",
            _ => code.ToString()
        };
    }

}
