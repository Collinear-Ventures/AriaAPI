// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.API.Write;
using AriaAPI.Core;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Runtime.CompilerServices;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace AriaAPI.Tests.Write
{
    /// <summary>
    /// Tests for validation guards in <see cref="CareTeamWrite"/>.
    /// All cases throw before any FHIR call is made.
    /// </summary>
    public sealed class CareTeamWriteTests
    {
        private static ClientConfigurator UninitializedConfigurator() =>
            (ClientConfigurator)RuntimeHelpers.GetUninitializedObject(typeof(ClientConfigurator));

        // ── UpdateAsync ────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_NullConfigurator_ThrowsArgumentNullException()
        {
            var resource = new CareTeam { Id = "ct-1" };

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                CareTeamWrite.UpdateAsync(null!, resource, NullLogger.Instance));

            Assert.Equal("configurator", ex.ParamName);
        }

        [Fact]
        public async Task UpdateAsync_NullResource_ThrowsArgumentNullException()
        {
            var configurator = UninitializedConfigurator();

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                CareTeamWrite.UpdateAsync(configurator, null!, NullLogger.Instance));

            Assert.Equal("resource", ex.ParamName);
        }

        [Fact]
        public async Task UpdateAsync_MissingId_ThrowsArgumentException()
        {
            var configurator = UninitializedConfigurator();
            var resource = new CareTeam(); // Id is null

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                CareTeamWrite.UpdateAsync(configurator, resource, NullLogger.Instance));

            Assert.Contains("Id", ex.Message);
        }

        // ── UpsertAsync ────────────────────────────────────────────────────────

        [Fact]
        public async Task UpsertAsync_NullConfigurator_ThrowsArgumentNullException()
        {
            var resource = new CareTeam { Id = "ct-1" };

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                CareTeamWrite.UpsertAsync(null!, resource, "identifier=urn:test|ct-1", NullLogger.Instance));

            Assert.Equal("configurator", ex.ParamName);
        }

        [Fact]
        public async Task UpsertAsync_NullResource_ThrowsArgumentNullException()
        {
            var configurator = UninitializedConfigurator();

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                CareTeamWrite.UpsertAsync(configurator, null!, "identifier=urn:test|ct-1", NullLogger.Instance));

            Assert.Equal("resource", ex.ParamName);
        }

        [Fact]
        public async Task UpsertAsync_NullIdentifier_ThrowsArgumentException()
        {
            var configurator = UninitializedConfigurator();
            var resource = new CareTeam();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                CareTeamWrite.UpsertAsync(configurator, resource, null!, NullLogger.Instance));
        }

        [Fact]
        public async Task UpsertAsync_WhitespaceIdentifier_ThrowsArgumentException()
        {
            var configurator = UninitializedConfigurator();
            var resource = new CareTeam();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                CareTeamWrite.UpsertAsync(configurator, resource, "   ", NullLogger.Instance));
        }

        // ── UpdateForPatientAsync ──────────────────────────────────────────────

        [Fact]
        public async Task UpdateForPatientAsync_NullConfigurator_ThrowsArgumentNullException()
        {
            var resource = new CareTeam { Id = "ct-1" };

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                CareTeamWrite.UpdateForPatientAsync(null!, "MRN001", resource, NullLogger.Instance));

            Assert.Equal("configurator", ex.ParamName);
        }

        [Fact]
        public async Task UpdateForPatientAsync_NullPatientIdentifier_ThrowsArgumentException()
        {
            var configurator = UninitializedConfigurator();
            var resource = new CareTeam { Id = "ct-1" };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                CareTeamWrite.UpdateForPatientAsync(configurator, null!, resource, NullLogger.Instance));
        }

        [Fact]
        public async Task UpdateForPatientAsync_NullResource_ThrowsArgumentNullException()
        {
            var configurator = UninitializedConfigurator();

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                CareTeamWrite.UpdateForPatientAsync(configurator, "MRN001", null!, NullLogger.Instance));

            Assert.Equal("resource", ex.ParamName);
        }

        // ── UpsertForPatientAsync ──────────────────────────────────────────────

        [Fact]
        public async Task UpsertForPatientAsync_NullConfigurator_ThrowsArgumentNullException()
        {
            var resource = new CareTeam();

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                CareTeamWrite.UpsertForPatientAsync(null!, "MRN001", resource, "identifier=urn:test|ct-1", NullLogger.Instance));

            Assert.Equal("configurator", ex.ParamName);
        }

        [Fact]
        public async Task UpsertForPatientAsync_NullPatientIdentifier_ThrowsArgumentException()
        {
            var configurator = UninitializedConfigurator();
            var resource = new CareTeam();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                CareTeamWrite.UpsertForPatientAsync(configurator, null!, resource, "identifier=urn:test|ct-1", NullLogger.Instance));
        }

        [Fact]
        public async Task UpsertForPatientAsync_NullIdentifier_ThrowsArgumentException()
        {
            var configurator = UninitializedConfigurator();
            var resource = new CareTeam();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                CareTeamWrite.UpsertForPatientAsync(configurator, "MRN001", resource, null!, NullLogger.Instance));
        }

        [Fact]
        public async Task UpsertForPatientAsync_WhitespaceIdentifier_ThrowsArgumentException()
        {
            var configurator = UninitializedConfigurator();
            var resource = new CareTeam();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                CareTeamWrite.UpsertForPatientAsync(configurator, "MRN001", resource, "   ", NullLogger.Instance));
        }
    }
}
