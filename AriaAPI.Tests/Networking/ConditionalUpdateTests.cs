// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.Networking.Core;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace AriaAPI.Tests.Networking
{
    /// <summary>
    /// Tests for <see cref="AriaFhirClient{T}.ConditionalUpdateAsync"/> guard conditions.
    /// </summary>
    public sealed class ConditionalUpdateTests
    {
        [Fact]
        public async Task ConditionalUpdateAsync_NullResource_ThrowsArgumentNullException()
        {
            var client = (AriaFhirClient<Patient>)RuntimeHelpers.GetUninitializedObject(typeof(AriaFhirClient<Patient>));

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                client.ConditionalUpdateAsync(null!, new SearchParams()));

            Assert.Equal("resource", ex.ParamName);
        }

        [Fact]
        public async Task ConditionalUpdateAsync_NullCondition_ThrowsArgumentNullException()
        {
            var client = (AriaFhirClient<Patient>)RuntimeHelpers.GetUninitializedObject(typeof(AriaFhirClient<Patient>));

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                client.ConditionalUpdateAsync(new Patient { Id = "p-1" }, null!));

            Assert.Equal("condition", ex.ParamName);
        }
    }
}
