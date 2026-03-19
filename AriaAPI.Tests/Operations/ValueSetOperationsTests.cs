// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.API.Operations;
using AriaAPI.Core;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace AriaAPI.Tests.Operations
{
    /// <summary>
    /// Tests for validation guards in <see cref="ValueSetOperations"/>.
    /// All cases throw before any FHIR call is made.
    /// </summary>
    public sealed class ValueSetOperationsTests
    {
        private static ClientConfigurator UninitializedConfigurator() =>
            (ClientConfigurator)RuntimeHelpers.GetUninitializedObject(typeof(ClientConfigurator));

        [Fact]
        public async Task ExpandAsync_NullConfigurator_ThrowsArgumentNullException()
        {
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                ValueSetOperations.ExpandAsync(null!, "http://example.com/vs"));

            Assert.Equal("configurator", ex.ParamName);
        }

        [Fact]
        public async Task ExpandAsync_NullUrl_ThrowsArgumentException()
        {
            var configurator = UninitializedConfigurator();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                ValueSetOperations.ExpandAsync(configurator, null!));
        }

        [Fact]
        public async Task ExpandAsync_WhitespaceUrl_ThrowsArgumentException()
        {
            var configurator = UninitializedConfigurator();

            await Assert.ThrowsAsync<ArgumentException>(() =>
                ValueSetOperations.ExpandAsync(configurator, "   "));
        }
    }
}
