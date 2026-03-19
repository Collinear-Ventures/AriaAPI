// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using AriaAPI.Core;
using Hl7.Fhir.Model;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;

namespace AriaAPI.Networking.Core
{
    /// <summary>
    /// Central factory for constructing FHIR API clients and search builders.
    /// </summary>
    public sealed class FhirFactory : IFhirFactory
    {
        private readonly IServiceProvider _provider;

        /// <summary>
        /// Initializes a new instance of the <see cref="FhirFactory"/> class.
        /// </summary>
        /// <param name="provider">The service provider used to resolve <see cref="ClientConfigurator"/> instances.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="provider"/> is <see langword="null"/>.</exception>
        public FhirFactory(IServiceProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        /// <summary>
        /// Creates a FHIR API client for the specified resource type.
        /// </summary>
        public AriaFhirClient<TResource> Client<TResource>(CancellationToken ct = default) where TResource : Resource
        {
            var configurator = _provider.GetRequiredService<ClientConfigurator>();
            return new AriaFhirClient<TResource>(configurator, ct);
        }

        /// <summary>
        /// Creates a fluent builder for FHIR SearchParams for the specified resource type.
        /// </summary>
        public Builder<TResource> Builder<TResource>() where TResource : Resource
            => new();
    }
}