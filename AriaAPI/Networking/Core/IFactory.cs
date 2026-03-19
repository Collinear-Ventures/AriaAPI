// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using AriaAPI.Core;
using Hl7.Fhir.Model;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;

namespace AriaAPI.Networking.Core
{
    /// <summary>
    /// Factory for creating typed FHIR client and builder instances pre-configured for the active system.
    /// </summary>
    public interface IFhirFactory
    {
        /// <summary>
        /// Creates an <see cref="AriaFhirClient{TResource}"/> for the specified FHIR resource type.
        /// </summary>
        /// <typeparam name="TResource">The FHIR resource type (e.g., <see cref="Hl7.Fhir.Model.Patient"/>).</typeparam>
        /// <param name="ct">Optional cancellation token propagated to all client operations.</param>
        /// <returns>A configured <see cref="AriaFhirClient{TResource}"/> ready for CRUD and search operations.</returns>
        AriaFhirClient<TResource> Client<TResource>(CancellationToken ct = default) where TResource : Resource;

        /// <summary>
        /// Creates a fluent <see cref="Builder{TResource}"/> for constructing typed FHIR search parameters.
        /// </summary>
        /// <typeparam name="TResource">The FHIR resource type to build search parameters for.</typeparam>
        /// <returns>A new <see cref="Builder{TResource}"/> instance.</returns>
        Builder<TResource> Builder<TResource>() where TResource : Resource;
    }


}