// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.Core;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using MRS = AriaAPI.API.MultiResourceSearch.MultiResourceSearch;

namespace AriaAPI.Tests.MultiResourceSearch
{
    /// <summary>
    /// Tests for <see cref="MRS.PatientAndEncountersByDateAsync"/> guard conditions.
    /// Live FHIR calls are not exercised.
    /// </summary>
    public sealed class PatientEncountersTests
    {
        private static ClientConfigurator UninitializedConfigurator() =>
            (ClientConfigurator)RuntimeHelpers.GetUninitializedObject(typeof(ClientConfigurator));

        private static readonly DateTimeOffset _start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset _end   = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

        [Fact]
        public async Task PatientAndEncountersByDateAsync_NullConfigurator_ThrowsArgumentNullException()
        {
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                MRS.PatientAndEncountersByDateAsync(null!, "MRN001", _start, _end));

            Assert.Equal("configurator", ex.ParamName);
        }

        [Fact]
        public async Task PatientAndEncountersByDateAsync_NullPatientIdentifier_ThrowsArgumentException()
        {
            var configurator = UninitializedConfigurator();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                MRS.PatientAndEncountersByDateAsync(configurator, null!, _start, _end));
        }

        [Fact]
        public async Task PatientAndEncountersByDateAsync_WhitespacePatientIdentifier_ThrowsArgumentException()
        {
            var configurator = UninitializedConfigurator();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                MRS.PatientAndEncountersByDateAsync(configurator, "   ", _start, _end));
        }
    }
}
