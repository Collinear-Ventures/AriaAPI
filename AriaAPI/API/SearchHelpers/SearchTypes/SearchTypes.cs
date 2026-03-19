// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using System;
using System.Collections.Generic;

namespace AriaAPI.API.SearchHelpers
{
    /// <summary>
    /// Provides strongly-typed enumerations and mapping dictionaries for FHIR search parameter values
    /// across multiple resource types (Appointment, Device, Location, Observation, etc.).
    /// </summary>
    public static partial class SearchTypes
    {

        /// <summary>
        /// Returns a display/search string for a given enum using the supplied map,
        /// falling back to the enum name if no mapping exists.
        /// </summary>
        public static string GetSearchValue<TEnum>(
            TEnum value,
            IReadOnlyDictionary<TEnum, string> map)
            where TEnum : struct, Enum
        {
            return map.TryGetValue(value, out var display) && !string.IsNullOrWhiteSpace(display)
                ? display
                : value.ToString();
        }

        /// <summary>
        /// Returns display/search strings for a sequence of enum values using the supplied map,
        /// preserving order, and falling back to enum names where needed.
        /// </summary>
        public static IEnumerable<string> GetSearchValues<TEnum>(
            IEnumerable<TEnum> values,
            IReadOnlyDictionary<TEnum, string> map)
            where TEnum : struct, Enum
        {
            foreach (var v in values)
                yield return GetSearchValue(v, map);
        }

        /// <summary>
        /// Try-pattern if you want to branch on “mapped vs fallback”.
        /// </summary>
        public static bool TryGetSearchValue<TEnum>(
            TEnum value,
            IReadOnlyDictionary<TEnum, string> map,
            out string display)
            where TEnum : struct, Enum
        {
            if (map.TryGetValue(value, out var s) && !string.IsNullOrWhiteSpace(s))
            {
                display = s;
                return true;
            }
            display = value.ToString();
            return false;
        }

    }
}
