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
    /// Tests for validation guards in <see cref="AppointmentWrite"/>.
    /// All cases throw before any FHIR call is made.
    /// </summary>
    public sealed class AppointmentWriteTests
    {
        private static ClientConfigurator UninitializedConfigurator() =>
            (ClientConfigurator)RuntimeHelpers.GetUninitializedObject(typeof(ClientConfigurator));

        // ── UpdateAsync ────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateAsync_NullConfigurator_ThrowsArgumentNullException()
        {
            var resource = new Appointment { Id = "appt-1" };

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                AppointmentWrite.UpdateAsync(null!, resource, NullLogger.Instance));

            Assert.Equal("configurator", ex.ParamName);
        }

        [Fact]
        public async Task UpdateAsync_NullResource_ThrowsArgumentNullException()
        {
            var configurator = UninitializedConfigurator();

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                AppointmentWrite.UpdateAsync(configurator, null!, NullLogger.Instance));

            Assert.Equal("resource", ex.ParamName);
        }

        [Fact]
        public async Task UpdateAsync_MissingId_ThrowsArgumentException()
        {
            var configurator = UninitializedConfigurator();
            var resource = new Appointment(); // Id is null

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                AppointmentWrite.UpdateAsync(configurator, resource, NullLogger.Instance));

            Assert.Contains("Id", ex.Message);
        }

        // ── UpsertAsync ────────────────────────────────────────────────────────

        [Fact]
        public async Task UpsertAsync_NullConfigurator_ThrowsArgumentNullException()
        {
            var resource = new Appointment { Id = "appt-1" };

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                AppointmentWrite.UpsertAsync(null!, resource, "identifier=urn:test|appt-1", NullLogger.Instance));

            Assert.Equal("configurator", ex.ParamName);
        }

        [Fact]
        public async Task UpsertAsync_NullResource_ThrowsArgumentNullException()
        {
            var configurator = UninitializedConfigurator();

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                AppointmentWrite.UpsertAsync(configurator, null!, "identifier=urn:test|appt-1", NullLogger.Instance));

            Assert.Equal("resource", ex.ParamName);
        }

        [Fact]
        public async Task UpsertAsync_NullIdentifier_ThrowsArgumentException()
        {
            var configurator = UninitializedConfigurator();
            var resource = new Appointment();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                AppointmentWrite.UpsertAsync(configurator, resource, null!, NullLogger.Instance));
        }

        [Fact]
        public async Task UpsertAsync_WhitespaceIdentifier_ThrowsArgumentException()
        {
            var configurator = UninitializedConfigurator();
            var resource = new Appointment();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                AppointmentWrite.UpsertAsync(configurator, resource, "   ", NullLogger.Instance));
        }

        // ── UpdateForPatientAsync ──────────────────────────────────────────────

        [Fact]
        public async Task UpdateForPatientAsync_NullConfigurator_ThrowsArgumentNullException()
        {
            var resource = new Appointment { Id = "appt-1" };

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                AppointmentWrite.UpdateForPatientAsync(null!, "MRN001", resource, NullLogger.Instance));

            Assert.Equal("configurator", ex.ParamName);
        }

        [Fact]
        public async Task UpdateForPatientAsync_NullPatientIdentifier_ThrowsArgumentException()
        {
            var configurator = UninitializedConfigurator();
            var resource = new Appointment { Id = "appt-1" };

            await Assert.ThrowsAsync<ArgumentException>(() =>
                AppointmentWrite.UpdateForPatientAsync(configurator, null!, resource, NullLogger.Instance));
        }

        [Fact]
        public async Task UpdateForPatientAsync_NullResource_ThrowsArgumentNullException()
        {
            var configurator = UninitializedConfigurator();

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                AppointmentWrite.UpdateForPatientAsync(configurator, "MRN001", null!, NullLogger.Instance));

            Assert.Equal("resource", ex.ParamName);
        }

        // ── UpsertForPatientAsync ──────────────────────────────────────────────

        [Fact]
        public async Task UpsertForPatientAsync_NullConfigurator_ThrowsArgumentNullException()
        {
            var resource = new Appointment();

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                AppointmentWrite.UpsertForPatientAsync(null!, "MRN001", resource, "identifier=urn:test|appt-1", NullLogger.Instance));

            Assert.Equal("configurator", ex.ParamName);
        }

        [Fact]
        public async Task UpsertForPatientAsync_NullPatientIdentifier_ThrowsArgumentException()
        {
            var configurator = UninitializedConfigurator();
            var resource = new Appointment();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                AppointmentWrite.UpsertForPatientAsync(configurator, null!, resource, "identifier=urn:test|appt-1", NullLogger.Instance));
        }

        [Fact]
        public async Task UpsertForPatientAsync_NullIdentifier_ThrowsArgumentException()
        {
            var configurator = UninitializedConfigurator();
            var resource = new Appointment();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                AppointmentWrite.UpsertForPatientAsync(configurator, "MRN001", resource, null!, NullLogger.Instance));
        }

        [Fact]
        public async Task UpsertForPatientAsync_WhitespaceIdentifier_ThrowsArgumentException()
        {
            var configurator = UninitializedConfigurator();
            var resource = new Appointment();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                AppointmentWrite.UpsertForPatientAsync(configurator, "MRN001", resource, "   ", NullLogger.Instance));
        }
    }
}
