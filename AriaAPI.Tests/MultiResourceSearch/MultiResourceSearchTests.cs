// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.Core;
using System;
using System.Threading.Tasks;
using Xunit;
using static AriaAPI.API.SearchHelpers.SearchTypes;
using MRS = AriaAPI.API.MultiResourceSearch.MultiResourceSearch;

namespace AriaAPI.Tests.Api.MultiResourceSearch
{
    /// <summary>
    /// Tests for <see cref="MRS"/> covering pure-logic paths:
    /// date-range validation and parameter-object construction.
    /// Live FHIR calls are not exercised — those require an integration test environment.
    /// </summary>
    /// <remarks>
    /// <see cref="MRS.ValidateDateRange"/> is <c>internal</c>; accessible via
    /// <c>InternalsVisibleTo("AriaAPI.Tests")</c> declared in the library csproj.
    /// </remarks>
    public sealed class MultiResourceSearchTests
    {
        // ── ValidateDateRange tests ────────────────────────────────────────────

        /// <summary>An end date in the future throws <see cref="ArgumentOutOfRangeException"/>.</summary>
        [Fact]
        public void ValidateDateRange_FutureEndDate_ThrowsArgumentOutOfRangeException()
        {
            var tomorrow = DateTime.Today.AddDays(1);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                MRS.ValidateDateRange(DateTime.Today.AddDays(-7), tomorrow, allDates: false));
        }

        /// <summary>StartDate later than EndDate throws <see cref="ArgumentException"/>.</summary>
        [Fact]
        public void ValidateDateRange_StartLaterThanEnd_ThrowsArgumentException()
        {
            var start = new DateTime(2026, 3, 10);
            var end = new DateTime(2026, 3, 1);

            Assert.Throws<ArgumentException>(() =>
                MRS.ValidateDateRange(start, end, allDates: false));
        }

        /// <summary>A range exceeding 730 days throws <see cref="ArgumentOutOfRangeException"/>.</summary>
        [Fact]
        public void ValidateDateRange_SpanExceeds730Days_ThrowsArgumentOutOfRangeException()
        {
            var start = new DateTime(2023, 1, 1);
            var end = new DateTime(2025, 1, 2); // 731 days — just over the limit

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                MRS.ValidateDateRange(start, end, allDates: false));
        }

        /// <summary>Exactly 730 days is within the allowed span and does not throw.</summary>
        [Fact]
        public void ValidateDateRange_SpanExactly730Days_DoesNotThrow()
        {
            var start = new DateTime(2024, 1, 1);
            var end = start.AddDays(730);
            if (end > DateTime.Today)
                return; // Skip if range extends into the future on this machine

            MRS.ValidateDateRange(start, end, allDates: false);
        }

        /// <summary>A valid date range within limits does not throw.</summary>
        [Fact]
        public void ValidateDateRange_ValidRange365Days_DoesNotThrow()
        {
            var start = new DateTime(2025, 1, 1);
            var end = new DateTime(2025, 12, 31);

            MRS.ValidateDateRange(start, end, allDates: false);
        }

        /// <summary>Both null dates (no bounds) are accepted without throwing.</summary>
        [Fact]
        public void ValidateDateRange_BothDatesNull_DoesNotThrow()
        {
            MRS.ValidateDateRange(null, null, allDates: false);
        }

        /// <summary>Only start date provided (no upper bound) is accepted without throwing.</summary>
        [Fact]
        public void ValidateDateRange_OnlyStartDate_DoesNotThrow()
        {
            MRS.ValidateDateRange(new DateTime(2025, 1, 1), null, allDates: false);
        }

        /// <summary>Only end date provided (no lower bound) does not throw when end is not in the future.</summary>
        [Fact]
        public void ValidateDateRange_OnlyEndDate_DoesNotThrow()
        {
            MRS.ValidateDateRange(null, new DateTime(2025, 12, 31), allDates: false);
        }

        /// <summary>When <c>allDates = true</c>, even an otherwise-invalid range is accepted.</summary>
        [Fact]
        public void ValidateDateRange_AllDatesTrue_SkipsAllChecks()
        {
            var futureEnd = DateTime.Today.AddYears(10);
            var start = futureEnd.AddDays(-3000); // span > 730, end in future

            MRS.ValidateDateRange(start, futureEnd, allDates: true);
        }

        // ── PatientDocumentsSearchParams constructor tests ─────────────────────

        /// <summary>Default-constructed params have expected null/default values.</summary>
        [Fact]
        public void PatientDocumentsSearchParams_DefaultConstructor_HasExpectedDefaults()
        {
            var p = new MRS.PatientDocumentsSearchParams();

            Assert.Null(p.PatientIdentifierOrName);
            Assert.Null(p.PatientId);
            Assert.Null(p.Types);
            Assert.Equal("current", p.Status);
            Assert.Null(p.DocStatus);
            Assert.Null(p.StartDate);
            Assert.Null(p.EndDate);
            Assert.False(p.AllDates);
            Assert.True(p.IncludeContent);
            Assert.True(p.SortByDateDescending);
            Assert.True(p.UsePatientLogicalIdWhenProvided);
            Assert.Null(p.Extra);
        }

        /// <summary>Properties set via object initializer are readable and retained.</summary>
        [Fact]
        public void PatientDocumentsSearchParams_ObjectInitializer_PropertiesAreSet()
        {
            var start = new DateTime(2026, 1, 1);
            var end = new DateTime(2026, 1, 31);
            var types = new[] { DocumentType.TreatmentPlan };

            var p = new MRS.PatientDocumentsSearchParams
            {
                PatientId = "patient-logical-id-abc",
                Types = types,
                StartDate = start,
                EndDate = end,
                AllDates = false,
                SortByDateDescending = false,
                ListReturnLimit = 100
            };

            Assert.Equal("patient-logical-id-abc", p.PatientId);
            Assert.Equal(types, p.Types);
            Assert.Equal(start, p.StartDate);
            Assert.Equal(end, p.EndDate);
            Assert.Equal(100, p.ListReturnLimit);
            Assert.False(p.SortByDateDescending);
        }

        // ── PatientWithDocumentsAsync null-guard test ──────────────────────────

        /// <summary>Passing a null <c>configurator</c> throws <see cref="ArgumentNullException"/>.</summary>
        [Fact]
        public async Task PatientWithDocumentsAsync_NullConfigurator_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                MRS.PatientWithDocumentsAsync(
                    null!,
                    new MRS.PatientDocumentsSearchParams { PatientId = "patient-abc" }));
        }

        /// <summary>
        /// When both identifiers are absent the method returns (null, empty) early.
        /// Tested via the null-configurator path which fires the configurator guard before the
        /// identifier guard — confirming the guard ordering in the method body.
        /// </summary>
        [Fact]
        public async Task PatientWithDocumentsAsync_NullParams_NullConfiguratorGuardFires()
        {
            // Both configurator and params null: ArgumentNullException for configurator fires first.
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                MRS.PatientWithDocumentsAsync(null!, new MRS.PatientDocumentsSearchParams()));
        }

        // ── PatientAndAppointmentsByDateAsync parameter guard ──────────────────

        /// <summary>Inverted date window (end &lt; start) throws <see cref="ArgumentException"/>.</summary>
        [Fact]
        public async Task PatientAndAppointmentsByDateAsync_EndBeforeStart_ThrowsArgumentException()
        {
            var start = new DateTimeOffset(2026, 3, 10, 0, 0, 0, TimeSpan.Zero);
            var end = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

            // The date-range guard fires before any object access on configurator.
            await Assert.ThrowsAsync<ArgumentException>(() =>
                MRS.PatientAndAppointmentsByDateAsync(null!, "patient-abc", start, end));
        }

        // ── ValidateDateRange integration through PatientWithDocumentsAsync ────

        /// <summary>
        /// Confirms the configurator null-check fires before the date-range guard — providing a
        /// seam to test deeper paths in integration tests once a real or mock configurator is available.
        /// </summary>
        [Fact]
        public async Task PatientWithDocumentsAsync_NullConfigurator_FiresBeforeDateValidation()
        {
            // Even with an invalid date range, the configurator null guard fires first.
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                MRS.PatientWithDocumentsAsync(
                    null!,
                    new MRS.PatientDocumentsSearchParams
                    {
                        PatientId = "patient-abc",
                        UsePatientLogicalIdWhenProvided = true,
                        StartDate = new DateTime(2026, 3, 10),
                        EndDate = new DateTime(2026, 3, 1) // inverted — invalid, but null-check fires first
                    }));
        }
    }
}
