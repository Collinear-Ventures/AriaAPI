// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using System;
using System.Threading.Tasks;
using AriaAPI.API.SearchHelpers;
using AriaAPI.API.SingleResourceSearch;
using Xunit;

namespace AriaAPI.Tests.SingleResourceSearch
{
    /// <summary>
    /// Tests for <see cref="ScheduleSearch"/> and <see cref="SlotSearch"/>.
    /// </summary>
    public sealed class ScheduleSlotSearchTests
    {
        // -----------------------------------------------------------------------
        // ScheduleSearch
        // -----------------------------------------------------------------------

        /// <summary>
        /// <see cref="ScheduleSearch.SearchSchedulesAsync"/> throws <see cref="ArgumentNullException"/>
        /// when a null configurator is supplied.
        /// </summary>
        [Fact]
        public async Task SearchSchedulesAsync_NullConfigurator_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                ScheduleSearch.SearchSchedulesAsync(null!, new ScheduleSearch.ScheduleSearchParams()));
        }

        /// <summary>
        /// Default <see cref="ScheduleSearch.ScheduleSearchParams.ListReturnLimit"/> equals
        /// <see cref="SearchExecutor.DefaultServerMaxResults"/>.
        /// </summary>
        [Fact]
        public void ScheduleSearchParams_DefaultListReturnLimit_Is500()
        {
            var p = new ScheduleSearch.ScheduleSearchParams();
            Assert.Equal(SearchExecutor.DefaultServerMaxResults, p.ListReturnLimit);
        }

        // -----------------------------------------------------------------------
        // SlotSearch
        // -----------------------------------------------------------------------

        /// <summary>
        /// <see cref="SlotSearch.SearchSlotsAsync"/> throws <see cref="ArgumentNullException"/>
        /// when a null configurator is supplied.
        /// </summary>
        [Fact]
        public async Task SearchSlotsAsync_NullConfigurator_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                SlotSearch.SearchSlotsAsync(null!, new SlotSearch.SlotSearchParams()));
        }

        /// <summary>
        /// Default <see cref="SlotSearch.SlotSearchParams.ListReturnLimit"/> equals
        /// <see cref="SearchExecutor.DefaultServerMaxResults"/>.
        /// </summary>
        [Fact]
        public void SlotSearchParams_DefaultListReturnLimit_Is500()
        {
            var p = new SlotSearch.SlotSearchParams();
            Assert.Equal(SearchExecutor.DefaultServerMaxResults, p.ListReturnLimit);
        }
    }
}
