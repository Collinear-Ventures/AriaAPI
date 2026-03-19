// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AriaAPI.API.SearchHelpers
{

    /// <summary>
    /// Extension methods that convert <see cref="SearchTypes"/> enum values to their
    /// FHIR-compatible search/display strings using the corresponding mapping dictionaries.
    /// </summary>
    public static class SearchTypeExtensions
    {

        /// <summary>
        /// Gets the display/search string for a <see cref="SearchTypes.DocumentType"/> value.
        /// Falls back to the enum name if no mapping exists.
        /// </summary>
        public static string ToSearchValue(this SearchTypes.DocumentType value)
            => SearchTypes.GetSearchValue(value, SearchTypes.DocumentTypeMap);

        /// <summary>
        /// Gets the display/search string for a <see cref="SearchTypes.AppointmentCategory"/> value.
        /// Falls back to the enum name if no mapping exists.
        /// </summary>
        public static string ToSearchValue(this SearchTypes.AppointmentCategory value)
            => SearchTypes.GetSearchValue(value, SearchTypes.AppointmentCategoryMap);

        /// <summary>
        /// Try-pattern for DocumentType: returns true if a mapped value exists (and is not whitespace),
        /// otherwise returns false and sets <paramref name="display"/> to the enum name.
        /// </summary>
        public static bool TryToSearchValue(
            this SearchTypes.DocumentType value,
            out string display)
            => SearchTypes.TryGetSearchValue(value, SearchTypes.DocumentTypeMap, out display);

        /// <summary>
        /// Try-pattern for AppointmentCategory: returns true if a mapped value exists (and is not whitespace),
        /// otherwise returns false and sets <paramref name="display"/> to the enum name.
        /// </summary>
        public static bool TryToSearchValue(
            this SearchTypes.AppointmentCategory value,
            out string display)
            => SearchTypes.TryGetSearchValue(value, SearchTypes.AppointmentCategoryMap, out display);

        /// <summary>
        /// Batch conversion for DocumentType values.
        /// </summary>
        public static IEnumerable<string> ToSearchValues(
            this IEnumerable<SearchTypes.DocumentType> values)
            => SearchTypes.GetSearchValues(values, SearchTypes.DocumentTypeMap);

        /// <summary>
        /// Batch conversion for AppointmentCategory values.
        /// </summary>
        public static IEnumerable<string> ToSearchValues(
            this IEnumerable<SearchTypes.AppointmentCategory> values)
            => SearchTypes.GetSearchValues(values, SearchTypes.AppointmentCategoryMap);


        /// <summary>
        /// Generic single-value conversion using a caller-supplied map.
        /// </summary>
        public static string ToSearchValue<TEnum>(
            this TEnum value,
            IReadOnlyDictionary<TEnum, string> map)
            where TEnum : struct, Enum
            => SearchTypes.GetSearchValue(value, map);

        /// <summary>
        /// Generic batch conversion using a caller-supplied map.
        /// </summary>
        public static IEnumerable<string> ToSearchValues<TEnum>(
            this IEnumerable<TEnum> values,
            IReadOnlyDictionary<TEnum, string> map)
            where TEnum : struct, Enum
            => SearchTypes.GetSearchValues(values, map);
    }

}
