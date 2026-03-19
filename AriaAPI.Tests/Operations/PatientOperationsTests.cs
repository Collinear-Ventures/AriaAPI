// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.API.Operations;
using AriaAPI.Core;
using Hl7.Fhir.Rest;
using RichardSzalay.MockHttp;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace AriaAPI.Tests.Operations
{
    /// <summary>
    /// Tests for <see cref="PatientOperations"/>.
    /// Guard tests verify behaviour before any FHIR call is made.
    /// Pagination tests use a mock HTTP handler to verify Bundle traversal
    /// and <c>listReturnLimit</c> trim behaviour.
    /// </summary>
    public sealed class PatientOperationsTests
    {
        // ── guard helpers ──────────────────────────────────────────────────────

        private static ClientConfigurator UninitializedConfigurator() =>
            (ClientConfigurator)RuntimeHelpers.GetUninitializedObject(typeof(ClientConfigurator));

        // ── guard tests ────────────────────────────────────────────────────────

        [Fact]
        public async Task EverythingAsync_NullConfigurator_ThrowsArgumentNullException()
        {
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                PatientOperations.EverythingAsync(null!, "MRN001"));

            Assert.Equal("configurator", ex.ParamName);
        }

        [Fact]
        public async Task EverythingAsync_NullPatientIdentifier_ThrowsArgumentException()
        {
            var configurator = UninitializedConfigurator();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                PatientOperations.EverythingAsync(configurator, null!));
        }

        [Fact]
        public async Task EverythingAsync_WhitespacePatientIdentifier_ThrowsArgumentException()
        {
            var configurator = UninitializedConfigurator();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                PatientOperations.EverythingAsync(configurator, "   "));
        }

        // ── pagination / mock-HTTP tests ───────────────────────────────────────

        private const string BaseUrl = "http://fhir.test.local/fhir/";

        /// <summary>
        /// Injects a pre-constructed <see cref="FhirClient"/> into an uninitialized
        /// <see cref="ClientConfigurator"/> via reflection, bypassing the constructor
        /// to avoid DI/auth dependencies while still exercising the full
        /// <see cref="PatientOperations"/> code path.
        /// </summary>
        private static ClientConfigurator MakeConfigurator(FhirClient fhirClient)
        {
            var configurator = UninitializedConfigurator();
            typeof(ClientConfigurator)
                .GetField("_fhirClient", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(configurator, fhirClient);
            return configurator;
        }

        private static HttpResponseMessage FhirJsonOk(string json) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/fhir+json")
            };

        // Minimal FHIR Bundle JSON that the Firely SDK can parse.
        private static string PatientBundleJson(string id) =>
            $"{{\"resourceType\":\"Bundle\",\"type\":\"searchset\",\"total\":1," +
            $"\"entry\":[{{\"resource\":{{\"resourceType\":\"Patient\",\"id\":\"{id}\"}}}}]}}";

        private static string EmptySearchBundleJson() =>
            "{\"resourceType\":\"Bundle\",\"type\":\"searchset\",\"total\":0,\"entry\":[]}";

        private static string EverythingBundleJson(int observationCount) =>
            "{\"resourceType\":\"Bundle\",\"type\":\"collection\",\"entry\":[" +
            string.Join(",",
                System.Linq.Enumerable.Range(1, observationCount).Select(i =>
                    $"{{\"resource\":{{\"resourceType\":\"Observation\",\"id\":\"obs-{i}\"}}}}")) +
            "]}";

        /// <summary>
        /// A single-page <c>$everything</c> Bundle with no next link returns all entries.
        /// </summary>
        [Fact]
        public async Task EverythingAsync_SinglePageBundle_ReturnsAllEntries()
        {
            var mockHttp = new MockHttpMessageHandler();

            // Register $everything before the generic Patient search so it matches first.
            mockHttp.When(HttpMethod.Get, $"{BaseUrl}Patient/p-001/$everything")
                    .Respond(_ => FhirJsonOk(EverythingBundleJson(2)));
            mockHttp.When(HttpMethod.Get, $"{BaseUrl}Patient*")
                    .Respond(_ => FhirJsonOk(PatientBundleJson("p-001")));

            var fhirClient = new FhirClient(BaseUrl, messageHandler: mockHttp);
            var configurator = MakeConfigurator(fhirClient);

            var result = await PatientOperations.EverythingAsync(configurator, "MRN001");

            Assert.Equal(2, result.Count);
        }

        /// <summary>
        /// When the number of entries returned by <c>$everything</c> exceeds
        /// <paramref name="listReturnLimit"/>, the result is trimmed to that limit.
        /// </summary>
        [Fact]
        public async Task EverythingAsync_ResultsExceedListReturnLimit_TrimsToLimit()
        {
            var mockHttp = new MockHttpMessageHandler();

            // Bundle returns 4 entries; limit is 2.
            mockHttp.When(HttpMethod.Get, $"{BaseUrl}Patient/p-001/$everything")
                    .Respond(_ => FhirJsonOk(EverythingBundleJson(4)));
            mockHttp.When(HttpMethod.Get, $"{BaseUrl}Patient*")
                    .Respond(_ => FhirJsonOk(PatientBundleJson("p-001")));

            var fhirClient = new FhirClient(BaseUrl, messageHandler: mockHttp);
            var configurator = MakeConfigurator(fhirClient);

            var result = await PatientOperations.EverythingAsync(configurator, "MRN001", listReturnLimit: 2);

            Assert.Equal(2, result.Count);
        }

        /// <summary>
        /// When no patient matches the identifier, <see cref="PatientOperations.EverythingAsync"/>
        /// returns an empty list without invoking the <c>$everything</c> operation.
        /// </summary>
        [Fact]
        public async Task EverythingAsync_PatientNotFound_ReturnsEmptyList()
        {
            var mockHttp = new MockHttpMessageHandler();

            mockHttp.When(HttpMethod.Get, $"{BaseUrl}Patient*")
                    .Respond(_ => FhirJsonOk(EmptySearchBundleJson()));

            var fhirClient = new FhirClient(BaseUrl, messageHandler: mockHttp);
            var configurator = MakeConfigurator(fhirClient);

            var result = await PatientOperations.EverythingAsync(configurator, "UNKNOWN-MRN");

            Assert.Empty(result);
        }
    }
}
