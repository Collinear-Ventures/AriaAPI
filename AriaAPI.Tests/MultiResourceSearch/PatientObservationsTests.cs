// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.Core;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using MRS = AriaAPI.API.MultiResourceSearch.MultiResourceSearch;

namespace AriaAPI.Tests.MultiResourceSearch
{
    /// <summary>
    /// Tests for <see cref="MRS.PatientAndObservationsByDateAsync"/> guard conditions.
    /// Live FHIR calls are not exercised.
    /// </summary>
    public sealed class PatientObservationsTests
    {
        private static ClientConfigurator UninitializedConfigurator() =>
            (ClientConfigurator)RuntimeHelpers.GetUninitializedObject(typeof(ClientConfigurator));

        private static readonly DateTimeOffset _start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset _end   = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);

        [Fact]
        public async Task PatientAndObservationsByDateAsync_NullConfigurator_ThrowsArgumentNullException()
        {
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                MRS.PatientAndObservationsByDateAsync(null!, "MRN001", _start, _end));

            Assert.Equal("configurator", ex.ParamName);
        }

        [Fact]
        public async Task PatientAndObservationsByDateAsync_NullPatientIdentifier_ThrowsArgumentException()
        {
            var configurator = UninitializedConfigurator();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                MRS.PatientAndObservationsByDateAsync(configurator, null!, _start, _end));
        }

        [Fact]
        public async Task PatientAndObservationsByDateAsync_WhitespacePatientIdentifier_ThrowsArgumentException()
        {
            var configurator = UninitializedConfigurator();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                MRS.PatientAndObservationsByDateAsync(configurator, "   ", _start, _end));
        }
    }
}
