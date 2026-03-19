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
    /// Tests for <see cref="CoverageSearch"/> and <see cref="ImmunizationSearch"/>.
    /// </summary>
    public sealed class CoverageImmunizationSearchTests
    {
        // -----------------------------------------------------------------------
        // CoverageSearch
        // -----------------------------------------------------------------------

        /// <summary>
        /// <see cref="CoverageSearch.SearchCoveragesAsync"/> throws <see cref="ArgumentNullException"/>
        /// when a null configurator is supplied.
        /// </summary>
        [Fact]
        public async Task SearchCoveragesAsync_NullConfigurator_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                CoverageSearch.SearchCoveragesAsync(null!, new CoverageSearch.CoverageSearchParams()));
        }

        /// <summary>
        /// Default <see cref="CoverageSearch.CoverageSearchParams.ListReturnLimit"/> equals
        /// <see cref="SearchExecutor.DefaultServerMaxResults"/>.
        /// </summary>
        [Fact]
        public void CoverageSearchParams_DefaultListReturnLimit_Is500()
        {
            var p = new CoverageSearch.CoverageSearchParams();
            Assert.Equal(SearchExecutor.DefaultServerMaxResults, p.ListReturnLimit);
        }

        /// <summary>
        /// <see cref="SearchTypes.CoverageStatusToToken"/> returns a non-empty string for every
        /// <see cref="CoverageStatus"/> value.
        /// </summary>
        [Fact]
        public void CoverageStatusToToken_AllValues_ReturnNonEmpty()
        {
            foreach (CoverageStatus s in Enum.GetValues<CoverageStatus>())
                Assert.False(string.IsNullOrWhiteSpace(SearchTypes.CoverageStatusToToken(s)),
                    $"CoverageStatusToToken returned null/empty for {s}");
        }

        // -----------------------------------------------------------------------
        // ImmunizationSearch
        // -----------------------------------------------------------------------

        /// <summary>
        /// <see cref="ImmunizationSearch.SearchImmunizationsAsync"/> throws <see cref="ArgumentNullException"/>
        /// when a null configurator is supplied.
        /// </summary>
        [Fact]
        public async Task SearchImmunizationsAsync_NullConfigurator_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                ImmunizationSearch.SearchImmunizationsAsync(null!, new ImmunizationSearch.ImmunizationSearchParams()));
        }

        /// <summary>
        /// Default <see cref="ImmunizationSearch.ImmunizationSearchParams.ListReturnLimit"/> equals
        /// <see cref="SearchExecutor.DefaultServerMaxResults"/>.
        /// </summary>
        [Fact]
        public void ImmunizationSearchParams_DefaultListReturnLimit_Is500()
        {
            var p = new ImmunizationSearch.ImmunizationSearchParams();
            Assert.Equal(SearchExecutor.DefaultServerMaxResults, p.ListReturnLimit);
        }
    }
}
