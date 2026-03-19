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
    /// Tests for <see cref="DiagnosticReportSearch"/> and <see cref="ImagingStudySearch"/>.
    /// </summary>
    public sealed class DiagnosticImagingSearchTests
    {
        // -----------------------------------------------------------------------
        // DiagnosticReportSearch
        // -----------------------------------------------------------------------

        /// <summary>
        /// <see cref="DiagnosticReportSearch.SearchDiagnosticReportsAsync"/> throws
        /// <see cref="ArgumentNullException"/> when a null configurator is supplied.
        /// </summary>
        [Fact]
        public async Task SearchDiagnosticReportsAsync_NullConfigurator_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                DiagnosticReportSearch.SearchDiagnosticReportsAsync(null!, new DiagnosticReportSearch.DiagnosticReportSearchParams()));
        }

        /// <summary>
        /// Default <see cref="DiagnosticReportSearch.DiagnosticReportSearchParams.ListReturnLimit"/> equals
        /// <see cref="SearchExecutor.DefaultServerMaxResults"/>.
        /// </summary>
        [Fact]
        public void DiagnosticReportSearchParams_DefaultListReturnLimit_Is500()
        {
            var p = new DiagnosticReportSearch.DiagnosticReportSearchParams();
            Assert.Equal(SearchExecutor.DefaultServerMaxResults, p.ListReturnLimit);
        }

        /// <summary>
        /// <see cref="SearchTypes.DiagnosticReportStatusToToken"/> returns a non-empty string for every
        /// <see cref="DiagnosticReportStatus"/> value.
        /// </summary>
        [Fact]
        public void DiagnosticReportStatusToToken_AllValues_ReturnNonEmpty()
        {
            foreach (DiagnosticReportStatus s in Enum.GetValues<DiagnosticReportStatus>())
                Assert.False(string.IsNullOrWhiteSpace(SearchTypes.DiagnosticReportStatusToToken(s)),
                    $"DiagnosticReportStatusToToken returned null/empty for {s}");
        }

        // -----------------------------------------------------------------------
        // ImagingStudySearch
        // -----------------------------------------------------------------------

        /// <summary>
        /// <see cref="ImagingStudySearch.SearchImagingStudiesAsync"/> throws
        /// <see cref="ArgumentNullException"/> when a null configurator is supplied.
        /// </summary>
        [Fact]
        public async Task SearchImagingStudiesAsync_NullConfigurator_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                ImagingStudySearch.SearchImagingStudiesAsync(null!, new ImagingStudySearch.ImagingStudySearchParams()));
        }

        /// <summary>
        /// Default <see cref="ImagingStudySearch.ImagingStudySearchParams.ListReturnLimit"/> equals
        /// <see cref="SearchExecutor.DefaultServerMaxResults"/>.
        /// </summary>
        [Fact]
        public void ImagingStudySearchParams_DefaultListReturnLimit_Is500()
        {
            var p = new ImagingStudySearch.ImagingStudySearchParams();
            Assert.Equal(SearchExecutor.DefaultServerMaxResults, p.ListReturnLimit);
        }

        /// <summary>
        /// <see cref="SearchTypes.ImagingStudyStatusToToken"/> returns a non-empty string for every
        /// <see cref="ImagingStudyStatus"/> value.
        /// </summary>
        [Fact]
        public void ImagingStudyStatusToToken_AllValues_ReturnNonEmpty()
        {
            foreach (ImagingStudyStatus s in Enum.GetValues<ImagingStudyStatus>())
                Assert.False(string.IsNullOrWhiteSpace(SearchTypes.ImagingStudyStatusToToken(s)),
                    $"ImagingStudyStatusToToken returned null/empty for {s}");
        }
    }
}
