// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using System;
using System.Collections.Generic;
using System.Linq;
using AriaAPI.API.SearchHelpers;
using Xunit;

namespace AriaAPI.Tests.SearchHelpers
{
    /// <summary>
    /// Tests for <see cref="SearchTypes"/> helper methods and the
    /// <see cref="SearchTypes.AppointmentCategoryMap"/> dictionary.
    /// </summary>
    public sealed class SearchTypesTests
    {
        // -----------------------------------------------------------------------
        // GetSearchValue / TryGetSearchValue
        // -----------------------------------------------------------------------

        private static readonly IReadOnlyDictionary<SearchTypes.AppointmentCategory, string> _map =
            SearchTypes.AppointmentCategoryMap;

        /// <summary>
        /// <see cref="SearchTypes.GetSearchValue{TEnum}"/> returns the mapped display string
        /// for an enum value that is present in the map.
        /// </summary>
        [Fact]
        public void GetSearchValue_MappedValue_ReturnsDisplay()
        {
            var display = SearchTypes.GetSearchValue(SearchTypes.AppointmentCategory.Treatment, _map);
            Assert.Equal("Treatment", display);
        }

        /// <summary>
        /// <see cref="SearchTypes.GetSearchValue{TEnum}"/> falls back to the enum's
        /// <see cref="Enum.ToString()"/> name when the value is not in the map.
        /// </summary>
        [Fact]
        public void GetSearchValue_UnmappedValue_ReturnsFallback()
        {
            // Use a fresh map that deliberately lacks the value.
            var emptyMap = new Dictionary<SearchTypes.AppointmentCategory, string>();
            var display = SearchTypes.GetSearchValue(SearchTypes.AppointmentCategory.Treatment, emptyMap);

            // Fallback = enum name
            Assert.Equal("Treatment", display);
        }

        /// <summary>
        /// <see cref="SearchTypes.TryGetSearchValue{TEnum}"/> returns <c>true</c> and the mapped
        /// display string when the enum value is present in the map.
        /// </summary>
        [Fact]
        public void TryGetSearchValue_MappedValue_ReturnsTrueAndDisplay()
        {
            var found = SearchTypes.TryGetSearchValue(
                SearchTypes.AppointmentCategory.Simulation,
                _map,
                out var display);

            Assert.True(found);
            Assert.Equal("Simulation", display);
        }

        /// <summary>
        /// <see cref="SearchTypes.TryGetSearchValue{TEnum}"/> returns <c>false</c> and the enum
        /// name as fallback when the value is not present in the map.
        /// </summary>
        [Fact]
        public void TryGetSearchValue_UnmappedValue_ReturnsFalseAndFallback()
        {
            var emptyMap = new Dictionary<SearchTypes.AppointmentCategory, string>();
            var found = SearchTypes.TryGetSearchValue(
                SearchTypes.AppointmentCategory.Dosimetry,
                emptyMap,
                out var display);

            Assert.False(found);
            Assert.Equal("Dosimetry", display);
        }

        // -----------------------------------------------------------------------
        // AppointmentCategoryMap spot-checks
        // -----------------------------------------------------------------------

        /// <summary>
        /// <see cref="SearchTypes.AppointmentCategory.Treatment"/> maps to "Treatment".
        /// </summary>
        [Fact]
        public void AppointmentCategoryMap_Treatment_ReturnsExpected()
        {
            Assert.Equal("Treatment", _map[SearchTypes.AppointmentCategory.Treatment]);
        }

        /// <summary>
        /// <see cref="SearchTypes.AppointmentCategory.ChartRounds"/> maps to "Chart Rounds"
        /// (a two-word value with a space).
        /// </summary>
        [Fact]
        public void AppointmentCategoryMap_ChartRounds_ReturnsWithSpace()
        {
            Assert.Equal("Chart Rounds", _map[SearchTypes.AppointmentCategory.ChartRounds]);
        }

        /// <summary>
        /// <see cref="SearchTypes.AppointmentCategory.CPortFilm"/> maps to "C-Port Film"
        /// (a hyphenated value).
        /// </summary>
        [Fact]
        public void AppointmentCategoryMap_CPortFilm_ReturnsWithHyphen()
        {
            Assert.Equal("C-Port Film", _map[SearchTypes.AppointmentCategory.CPortFilm]);
        }

        /// <summary>
        /// The <see cref="SearchTypes.AppointmentCategoryMap"/> contains an entry for every
        /// member of <see cref="SearchTypes.AppointmentCategory"/>.
        /// </summary>
        [Fact]
        public void AppointmentCategoryMap_AllValuesPresent()
        {
            var allValues = Enum.GetValues(typeof(SearchTypes.AppointmentCategory))
                .Cast<SearchTypes.AppointmentCategory>()
                .ToList();

            foreach (var value in allValues)
            {
                Assert.True(
                    _map.ContainsKey(value),
                    $"AppointmentCategoryMap is missing an entry for '{value}'.");
            }
        }

        /// <summary>
        /// <see cref="SearchTypes.GetSearchValues{TEnum}"/> returns display strings in the
        /// same order as the input sequence.
        /// </summary>
        [Fact]
        public void GetSearchValues_PreservesOrder()
        {
            var input = new[]
            {
                SearchTypes.AppointmentCategory.Treatment,
                SearchTypes.AppointmentCategory.Simulation,
                SearchTypes.AppointmentCategory.ChartRounds
            };

            var results = SearchTypes.GetSearchValues(input, _map).ToList();

            Assert.Equal(3, results.Count);
            Assert.Equal("Treatment", results[0]);
            Assert.Equal("Simulation", results[1]);
            Assert.Equal("Chart Rounds", results[2]);
        }

        /// <summary>
        /// <see cref="SearchTypes.AppointmentCategory.PreSimulation"/> maps to "Pre-Simulation"
        /// (a hyphenated value).
        /// </summary>
        [Fact]
        public void AppointmentCategoryMap_PreSimulation_ReturnsWithHyphen()
        {
            Assert.Equal("Pre-Simulation", _map[SearchTypes.AppointmentCategory.PreSimulation]);
        }
    }
}
