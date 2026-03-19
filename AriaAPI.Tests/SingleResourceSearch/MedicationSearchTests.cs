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
    /// Tests for <see cref="MedicationRequestSearch"/> and <see cref="MedicationAdministrationSearch"/>.
    /// </summary>
    public sealed class MedicationSearchTests
    {
        // -----------------------------------------------------------------------
        // MedicationRequestSearch
        // -----------------------------------------------------------------------

        /// <summary>
        /// <see cref="MedicationRequestSearch.SearchMedicationRequestsAsync"/> throws
        /// <see cref="ArgumentNullException"/> when a null configurator is supplied.
        /// </summary>
        [Fact]
        public async Task SearchMedicationRequestsAsync_NullConfigurator_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                MedicationRequestSearch.SearchMedicationRequestsAsync(null!, new MedicationRequestSearch.MedicationRequestSearchParams()));
        }

        /// <summary>
        /// Default <see cref="MedicationRequestSearch.MedicationRequestSearchParams.ListReturnLimit"/> equals
        /// <see cref="SearchExecutor.DefaultServerMaxResults"/>.
        /// </summary>
        [Fact]
        public void MedicationRequestSearchParams_DefaultListReturnLimit_Is500()
        {
            var p = new MedicationRequestSearch.MedicationRequestSearchParams();
            Assert.Equal(SearchExecutor.DefaultServerMaxResults, p.ListReturnLimit);
        }

        /// <summary>
        /// <see cref="SearchTypes.MedicationRequestStatusToToken"/> returns a non-empty string for every
        /// <see cref="MedicationRequestStatus"/> value.
        /// </summary>
        [Fact]
        public void MedicationRequestStatusToToken_AllValues_ReturnNonEmpty()
        {
            foreach (MedicationRequestStatus s in Enum.GetValues<MedicationRequestStatus>())
                Assert.False(string.IsNullOrWhiteSpace(SearchTypes.MedicationRequestStatusToToken(s)),
                    $"MedicationRequestStatusToToken returned null/empty for {s}");
        }

        // -----------------------------------------------------------------------
        // MedicationAdministrationSearch
        // -----------------------------------------------------------------------

        /// <summary>
        /// <see cref="MedicationAdministrationSearch.SearchMedicationAdministrationsAsync"/> throws
        /// <see cref="ArgumentNullException"/> when a null configurator is supplied.
        /// </summary>
        [Fact]
        public async Task SearchMedicationAdministrationsAsync_NullConfigurator_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                MedicationAdministrationSearch.SearchMedicationAdministrationsAsync(null!, new MedicationAdministrationSearch.MedicationAdministrationSearchParams()));
        }

        /// <summary>
        /// Default <see cref="MedicationAdministrationSearch.MedicationAdministrationSearchParams.ListReturnLimit"/> equals
        /// <see cref="SearchExecutor.DefaultServerMaxResults"/>.
        /// </summary>
        [Fact]
        public void MedicationAdministrationSearchParams_DefaultListReturnLimit_Is500()
        {
            var p = new MedicationAdministrationSearch.MedicationAdministrationSearchParams();
            Assert.Equal(SearchExecutor.DefaultServerMaxResults, p.ListReturnLimit);
        }

        /// <summary>
        /// <see cref="SearchTypes.MedicationAdministrationStatusToToken"/> returns a non-empty string for every
        /// <see cref="MedicationAdministrationStatus"/> value.
        /// </summary>
        [Fact]
        public void MedicationAdministrationStatusToToken_AllValues_ReturnNonEmpty()
        {
            foreach (MedicationAdministrationStatus s in Enum.GetValues<MedicationAdministrationStatus>())
                Assert.False(string.IsNullOrWhiteSpace(SearchTypes.MedicationAdministrationStatusToToken(s)),
                    $"MedicationAdministrationStatusToToken returned null/empty for {s}");
        }
    }
}
