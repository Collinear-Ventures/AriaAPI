// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using System;
using static AriaAPI.API.MultiResourceSearch.MultiResourceSearch;
using AriaAPI.API.SingleResourceSearch;
using Xunit;

namespace AriaAPI.Tests.Validation
{
    /// <summary>
    /// Unit tests for the internal input-validation helpers added in the v1.0.0-beta.1
    /// hardening pass: <see cref="PatientSearch.ValidateIdentifierInput"/> and
    /// <see cref="PatientDocuments.ValidateDateRange"/>.
    /// Both helpers are <c>internal</c> and accessible via the project's
    /// <c>InternalsVisibleTo("AriaAPI.Tests")</c> declaration.
    /// </summary>
    public sealed class InputValidationTests
    {
        // ── PatientSearch.ValidateIdentifierInput ──────────────────────────────

        /// <summary>Null is always valid — null signals "not supplied".</summary>
        [Fact]
        public void ValidateIdentifierInput_Null_DoesNotThrow()
        {
            PatientSearch.ValidateIdentifierInput(null, "p");
        }

        /// <summary>Empty string is valid (no content to reject).</summary>
        [Fact]
        public void ValidateIdentifierInput_EmptyString_DoesNotThrow()
        {
            PatientSearch.ValidateIdentifierInput("", "p");
        }

        /// <summary>A plain alphanumeric ID is valid.</summary>
        [Fact]
        public void ValidateIdentifierInput_SimpleAlphanumeric_DoesNotThrow()
        {
            PatientSearch.ValidateIdentifierInput("Patient-123", "p");
        }

        /// <summary>A FHIR system-qualified identifier (system|value) is valid.</summary>
        [Fact]
        public void ValidateIdentifierInput_FhirSystemQualifiedIdentifier_DoesNotThrow()
        {
            PatientSearch.ValidateIdentifierInput(
                "http://varian.com/fhir/identifier/Patient/ARIAID1|ID1", "p");
        }

        /// <summary>A string of exactly 200 characters is at the boundary and must be accepted.</summary>
        [Fact]
        public void ValidateIdentifierInput_ExactlyMaxLength_DoesNotThrow()
        {
            PatientSearch.ValidateIdentifierInput(new string('a', 200), "p");
        }

        /// <summary>A string of 201 characters exceeds the cap and must throw.</summary>
        [Fact]
        public void ValidateIdentifierInput_OneBeyondMaxLength_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => PatientSearch.ValidateIdentifierInput(new string('a', 201), "myParam"));
            Assert.Equal("myParam", ex.ParamName);
        }

        /// <summary>Characters outside the allowed set (e.g. backtick) must throw.</summary>
        [Fact]
        public void ValidateIdentifierInput_InvalidCharacter_ThrowsArgumentException()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => PatientSearch.ValidateIdentifierInput("bad`value", "myParam"));
            Assert.Equal("myParam", ex.ParamName);
        }

        /// <summary>Semicolon, a common injection vector, must be rejected.</summary>
        [Fact]
        public void ValidateIdentifierInput_Semicolon_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(
                () => PatientSearch.ValidateIdentifierInput("val;ue", "p"));
        }

        /// <summary>A realistic MRN-style identifier with hyphens and digits is valid.</summary>
        [Fact]
        public void ValidateIdentifierInput_MrnStyle_DoesNotThrow()
        {
            PatientSearch.ValidateIdentifierInput("MRN-00123456", "p");
        }

        // ── PatientSearch.ValidateIdentifierInput — via public PatientAsync(string _id) ──

        /// <summary>
        /// Calling the public <c>PatientAsync(configurator, _id)</c> overload with a too-long ID
        /// must throw <see cref="ArgumentException"/> before any network I/O occurs.
        /// Verification: <c>configurator</c> is null; if validation runs after the null check,
        /// we'd get <see cref="ArgumentNullException"/> instead.
        /// </summary>
        [Fact]
        public async System.Threading.Tasks.Task PatientAsync_StringId_TooLong_ThrowsBeforeNullCheck()
        {
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => PatientSearch.PatientAsync(null!, new string('a', 201)));
            Assert.Equal("_id", ex.ParamName);
        }

        /// <summary>
        /// Calling the public <c>PatientAsync(configurator, _id)</c> overload with an invalid
        /// character in the ID must throw <see cref="ArgumentException"/> before network I/O.
        /// </summary>
        [Fact]
        public async System.Threading.Tasks.Task PatientAsync_StringId_InvalidChar_ThrowsBeforeNullCheck()
        {
            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => PatientSearch.PatientAsync(null!, "bad`id"));
            Assert.Equal("_id", ex.ParamName);
        }

        // ── PatientDocuments.ValidateDateRange ────────────────────────────────

        /// <summary>When allDates is true, any date combination is accepted without validation.</summary>
        [Fact]
        public void ValidateDateRange_AllDatesTrue_AlwaysPasses()
        {
            // Future end date would normally throw — but allDates=true bypasses everything.
            ValidateDateRange(
                DateTime.Today.AddYears(-5),
                DateTime.Today.AddYears(5),
                allDates: true);
        }

        /// <summary>A valid recent range is accepted.</summary>
        [Fact]
        public void ValidateDateRange_ValidRange_DoesNotThrow()
        {
            ValidateDateRange(
                DateTime.Today.AddMonths(-6),
                DateTime.Today,
                allDates: false);
        }

        /// <summary>A same-day range (start == end) is valid.</summary>
        [Fact]
        public void ValidateDateRange_SameDayRange_DoesNotThrow()
        {
            var today = DateTime.Today;
            ValidateDateRange(today, today, allDates: false);
        }

        /// <summary>An end date in the future must throw ArgumentOutOfRangeException.</summary>
        [Fact]
        public void ValidateDateRange_FutureEndDate_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => ValidateDateRange(
                    DateTime.Today,
                    DateTime.Today.AddDays(1),
                    allDates: false));
        }

        /// <summary>start > end must throw ArgumentException.</summary>
        [Fact]
        public void ValidateDateRange_StartAfterEnd_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(
                () => ValidateDateRange(
                    DateTime.Today,
                    DateTime.Today.AddDays(-1),
                    allDates: false));
        }

        /// <summary>A span of exactly 730 days is at the boundary and must be accepted.</summary>
        [Fact]
        public void ValidateDateRange_ExactlyTwoYears_DoesNotThrow()
        {
            var end = DateTime.Today;
            var start = end.AddDays(-730);
            ValidateDateRange(start, end, allDates: false);
        }

        /// <summary>A span of 731 days exceeds the cap and must throw ArgumentOutOfRangeException.</summary>
        [Fact]
        public void ValidateDateRange_TwoYearsPlusOneDay_ThrowsArgumentOutOfRangeException()
        {
            var end = DateTime.Today;
            var start = end.AddDays(-731);
            Assert.Throws<ArgumentOutOfRangeException>(
                () => ValidateDateRange(start, end, allDates: false));
        }

        /// <summary>Only start provided (no end) — no cap applies to open-ended forward scans per current design.</summary>
        [Fact]
        public void ValidateDateRange_StartOnlyNoEnd_DoesNotThrow()
        {
            // The plan did not specify a cap for one-sided ranges; this documents the current behavior.
            ValidateDateRange(
                DateTime.Today.AddYears(-10),
                end: null,
                allDates: false);
        }

        /// <summary>Only end provided (no start) — no cap applies per current design.</summary>
        [Fact]
        public void ValidateDateRange_EndOnlyNoStart_DoesNotThrow()
        {
            // Same as above — one-sided ranges are not capped in the current implementation.
            ValidateDateRange(
                start: null,
                DateTime.Today,
                allDates: false);
        }
    }
}
