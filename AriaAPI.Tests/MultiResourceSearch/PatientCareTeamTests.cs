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
    /// Tests for <see cref="MRS.PatientAndCareTeamAsync"/> guard conditions.
    /// Live FHIR calls are not exercised.
    /// </summary>
    public sealed class PatientCareTeamTests
    {
        private static ClientConfigurator UninitializedConfigurator() =>
            (ClientConfigurator)RuntimeHelpers.GetUninitializedObject(typeof(ClientConfigurator));

        [Fact]
        public async Task PatientAndCareTeamAsync_NullConfigurator_ThrowsArgumentNullException()
        {
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                MRS.PatientAndCareTeamAsync(null!, "MRN001"));

            Assert.Equal("configurator", ex.ParamName);
        }

        [Fact]
        public async Task PatientAndCareTeamAsync_NullPatientIdentifier_ThrowsArgumentException()
        {
            var configurator = UninitializedConfigurator();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                MRS.PatientAndCareTeamAsync(configurator, null!));
        }

        [Fact]
        public async Task PatientAndCareTeamAsync_WhitespacePatientIdentifier_ThrowsArgumentException()
        {
            var configurator = UninitializedConfigurator();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                MRS.PatientAndCareTeamAsync(configurator, "   "));
        }
    }
}
