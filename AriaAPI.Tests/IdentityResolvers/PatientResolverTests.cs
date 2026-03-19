// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.API.IdentityResolvers;
using AriaAPI.Core;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace AriaAPI.Tests.IdentityResolvers
{
    /// <summary>
    /// Tests for <see cref="PatientResolver"/> constructor null guards.
    /// </summary>
    public sealed class PatientResolverTests
    {
        /// <summary>
        /// Returns a non-null <see cref="ClientConfigurator"/> whose fields are uninitialized.
        /// Safe to use only in tests where the guard fires before the configurator is dereferenced.
        /// </summary>
        private static ClientConfigurator UninitializedConfigurator() =>
            (ClientConfigurator)RuntimeHelpers.GetUninitializedObject(typeof(ClientConfigurator));

        [Fact]
        public void Constructor_NullConfigurator_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new PatientResolver(null!, NullLogger<PatientResolver>.Instance));

            Assert.Equal("configurator", ex.ParamName);
        }

        [Fact]
        public void Constructor_NullLogger_ThrowsArgumentNullException()
        {
            // configurator must be non-null to reach the logger null guard.
            // Use an uninitialized instance — it passes the null check but is never dereferenced
            // because the constructor throws on the logger guard immediately after.
            var configurator = UninitializedConfigurator();

            var ex = Assert.Throws<ArgumentNullException>(() =>
                new PatientResolver(configurator, null!));

            Assert.Equal("logger", ex.ParamName);
        }
    }
}
