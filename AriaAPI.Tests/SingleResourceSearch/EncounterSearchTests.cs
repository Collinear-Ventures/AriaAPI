// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using System;
using System.Threading.Tasks;
using AriaAPI.API.SearchHelpers;
using AriaAPI.API.SingleResourceSearch;
using Xunit;
using static AriaAPI.API.SearchHelpers.SearchTypes;

namespace AriaAPI.Tests.SingleResourceSearch
{
    /// <summary>
    /// Tests for <see cref="EncounterSearch"/>.
    /// </summary>
    public sealed class EncounterSearchTests
    {
        /// <summary>
        /// <see cref="EncounterSearch.SearchEncountersAsync"/> throws <see cref="ArgumentNullException"/>
        /// when a null configurator is supplied.
        /// </summary>
        [Fact]
        public async Task SearchEncountersAsync_NullConfigurator_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                EncounterSearch.SearchEncountersAsync(null!, new EncounterSearch.EncounterSearchParams()));
        }

        /// <summary>
        /// Default <see cref="EncounterSearch.EncounterSearchParams.ListReturnLimit"/> equals
        /// <see cref="SearchExecutor.DefaultServerMaxResults"/>.
        /// </summary>
        [Fact]
        public void SearchParams_DefaultListReturnLimit_Is500()
        {
            var p = new EncounterSearch.EncounterSearchParams();
            Assert.Equal(SearchExecutor.DefaultServerMaxResults, p.ListReturnLimit);
        }

        /// <summary>
        /// <see cref="SearchTypes.EncounterStatusToToken"/> returns a non-empty string for every
        /// <see cref="EncounterStatus"/> value.
        /// </summary>
        [Fact]
        public void EncounterStatusToToken_AllValues_ReturnNonEmpty()
        {
            foreach (EncounterStatus s in Enum.GetValues<EncounterStatus>())
                Assert.False(string.IsNullOrWhiteSpace(SearchTypes.EncounterStatusToToken(s)),
                    $"EncounterStatusToToken returned null/empty for {s}");
        }
    }
}
