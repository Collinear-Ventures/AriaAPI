// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using System;
using System.Threading.Tasks;
using AriaAPI.API.SearchHelpers;
using AriaAPI.API.SingleResourceSearch;
using Xunit;

namespace AriaAPI.Tests.SingleResourceSearch
{
    /// <summary>
    /// Tests for <see cref="PractitionerRoleSearch"/> and <see cref="RelatedPersonSearch"/>.
    /// </summary>
    public sealed class PractitionerRelatedSearchTests
    {
        // -----------------------------------------------------------------------
        // PractitionerRoleSearch
        // -----------------------------------------------------------------------

        /// <summary>
        /// <see cref="PractitionerRoleSearch.SearchPractitionerRolesAsync"/> throws
        /// <see cref="ArgumentNullException"/> when a null configurator is supplied.
        /// </summary>
        [Fact]
        public async Task SearchPractitionerRolesAsync_NullConfigurator_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                PractitionerRoleSearch.SearchPractitionerRolesAsync(null!, new PractitionerRoleSearch.PractitionerRoleSearchParams()));
        }

        /// <summary>
        /// Default <see cref="PractitionerRoleSearch.PractitionerRoleSearchParams.ListReturnLimit"/> equals
        /// <see cref="SearchExecutor.DefaultServerMaxResults"/>.
        /// </summary>
        [Fact]
        public void PractitionerRoleSearchParams_DefaultListReturnLimit_Is500()
        {
            var p = new PractitionerRoleSearch.PractitionerRoleSearchParams();
            Assert.Equal(SearchExecutor.DefaultServerMaxResults, p.ListReturnLimit);
        }

        // -----------------------------------------------------------------------
        // RelatedPersonSearch
        // -----------------------------------------------------------------------

        /// <summary>
        /// <see cref="RelatedPersonSearch.SearchRelatedPersonsAsync"/> throws
        /// <see cref="ArgumentNullException"/> when a null configurator is supplied.
        /// </summary>
        [Fact]
        public async Task SearchRelatedPersonsAsync_NullConfigurator_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                RelatedPersonSearch.SearchRelatedPersonsAsync(null!, new RelatedPersonSearch.RelatedPersonSearchParams()));
        }

        /// <summary>
        /// Default <see cref="RelatedPersonSearch.RelatedPersonSearchParams.ListReturnLimit"/> equals
        /// <see cref="SearchExecutor.DefaultServerMaxResults"/>.
        /// </summary>
        [Fact]
        public void RelatedPersonSearchParams_DefaultListReturnLimit_Is500()
        {
            var p = new RelatedPersonSearch.RelatedPersonSearchParams();
            Assert.Equal(SearchExecutor.DefaultServerMaxResults, p.ListReturnLimit);
        }
    }
}
