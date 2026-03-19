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
    /// Tests for <see cref="NutritionOrderSearch"/> and <see cref="RiskAssessmentSearch"/>.
    /// </summary>
    public sealed class NutritionRiskSearchTests
    {
        // -----------------------------------------------------------------------
        // NutritionOrderSearch
        // -----------------------------------------------------------------------

        /// <summary>
        /// <see cref="NutritionOrderSearch.SearchNutritionOrdersAsync"/> throws <see cref="ArgumentNullException"/>
        /// when a null configurator is supplied.
        /// </summary>
        [Fact]
        public async Task SearchNutritionOrdersAsync_NullConfigurator_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                NutritionOrderSearch.SearchNutritionOrdersAsync(null!, new NutritionOrderSearch.NutritionOrderSearchParams()));
        }

        /// <summary>
        /// Default <see cref="NutritionOrderSearch.NutritionOrderSearchParams.ListReturnLimit"/> equals
        /// <see cref="SearchExecutor.DefaultServerMaxResults"/>.
        /// </summary>
        [Fact]
        public void NutritionOrderSearchParams_DefaultListReturnLimit_Is500()
        {
            var p = new NutritionOrderSearch.NutritionOrderSearchParams();
            Assert.Equal(SearchExecutor.DefaultServerMaxResults, p.ListReturnLimit);
        }

        /// <summary>
        /// <see cref="SearchTypes.NutritionOrderStatusToToken"/> returns a non-empty string for every
        /// <see cref="NutritionOrderStatus"/> value.
        /// </summary>
        [Fact]
        public void NutritionOrderStatusToToken_AllValues_ReturnNonEmpty()
        {
            foreach (NutritionOrderStatus s in Enum.GetValues<NutritionOrderStatus>())
                Assert.False(string.IsNullOrWhiteSpace(SearchTypes.NutritionOrderStatusToToken(s)),
                    $"NutritionOrderStatusToToken returned null/empty for {s}");
        }

        // -----------------------------------------------------------------------
        // RiskAssessmentSearch
        // -----------------------------------------------------------------------

        /// <summary>
        /// <see cref="RiskAssessmentSearch.SearchRiskAssessmentsAsync"/> throws <see cref="ArgumentNullException"/>
        /// when a null configurator is supplied.
        /// </summary>
        [Fact]
        public async Task SearchRiskAssessmentsAsync_NullConfigurator_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                RiskAssessmentSearch.SearchRiskAssessmentsAsync(null!, new RiskAssessmentSearch.RiskAssessmentSearchParams()));
        }

        /// <summary>
        /// Default <see cref="RiskAssessmentSearch.RiskAssessmentSearchParams.ListReturnLimit"/> equals
        /// <see cref="SearchExecutor.DefaultServerMaxResults"/>.
        /// </summary>
        [Fact]
        public void RiskAssessmentSearchParams_DefaultListReturnLimit_Is500()
        {
            var p = new RiskAssessmentSearch.RiskAssessmentSearchParams();
            Assert.Equal(SearchExecutor.DefaultServerMaxResults, p.ListReturnLimit);
        }
    }
}
