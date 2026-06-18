// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.API.Create;
using AriaAPI.Core;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace AriaAPI.Tests.Create;

/// <summary>
/// Tests for validation guards in <see cref="TaskCreate.CreateAsync"/>.
/// All cases throw before any FHIR call is made.
/// </summary>
public sealed class TaskCreateTests
{
    /// <summary>
    /// Returns a non-null <see cref="ClientConfigurator"/> whose fields are uninitialized.
    /// Safe to use only in tests where the guard fires before the configurator is dereferenced.
    /// </summary>
    private static ClientConfigurator UninitializedConfigurator() =>
        (ClientConfigurator)RuntimeHelpers.GetUninitializedObject(typeof(ClientConfigurator));

    [Fact]
    public async Task CreateAsync_NullConfigurator_ThrowsArgumentNullException()
    {
        var p = new TaskCreate.TaskCreateParams { PatientReference = "Patient/123" };

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            TaskCreate.CreateAsync(null!, p, NullLogger.Instance));

        Assert.Equal("configurator", ex.ParamName);
    }

    [Fact]
    public async Task CreateAsync_NullParams_ThrowsArgumentNullException()
    {
        var configurator = UninitializedConfigurator();

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            TaskCreate.CreateAsync(configurator, null!, NullLogger.Instance));

        Assert.Equal("p", ex.ParamName);
    }

    [Fact]
    public async Task CreateAsync_EmptyPatientReference_ThrowsArgumentException()
    {
        var configurator = UninitializedConfigurator();
        var p = new TaskCreate.TaskCreateParams { PatientReference = string.Empty };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            TaskCreate.CreateAsync(configurator, p, NullLogger.Instance));

        Assert.Equal("PatientReference", ex.ParamName);
    }

    [Fact]
    public async Task CreateAsync_WhitespacePatientReference_ThrowsArgumentException()
    {
        var configurator = UninitializedConfigurator();
        var p = new TaskCreate.TaskCreateParams { PatientReference = "   " };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            TaskCreate.CreateAsync(configurator, p, NullLogger.Instance));

        Assert.Equal("PatientReference", ex.ParamName);
    }
}
