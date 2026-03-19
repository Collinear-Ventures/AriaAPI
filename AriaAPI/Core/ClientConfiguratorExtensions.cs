// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using AriaAPI.Networking.Core;
using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AriaAPI.Core
{
    /// <summary>
    /// Extension methods for <see cref="ClientConfigurator"/> that simplify the creation
    /// of type-safe FHIR resource clients.
    /// </summary>
    public static class ClientConfiguratorExtensions
    {

        /// <summary>
        /// Creates a generic <see cref="AriaFhirClient{TResource}"/> for the specified FHIR resource type using the provided <see cref="ClientConfigurator"/>.
        /// </summary>
        /// <typeparam name="TResource">
        /// The FHIR resource type (such as <see cref="Patient"/>, <see cref="DocumentReference"/>, <see cref="Appointment"/>, etc.).
        /// </typeparam>
        /// <param name="configurator">
        /// The <see cref="ClientConfigurator"/> instance that manages FHIR client configuration and authentication.
        /// </param>
        /// <param name="ct">
        /// Cancellation token forwarded to the underlying <see cref="AriaFhirClient{TResource}"/> and used
        /// for all HTTP calls it makes.
        /// </param>
        /// <returns>
        /// An <see cref="AriaFhirClient{TResource}"/> instance for the specified resource type, ready to perform FHIR operations.
        /// </returns>
        /// <remarks>
        /// This extension method simplifies the creation of type-safe FHIR clients for different resource types,
        /// allowing you to reuse the same <see cref="ClientConfigurator"/> for multiple searches.
        /// </remarks>

        public static AriaFhirClient<TResource> ForResource<TResource>(
                this ClientConfigurator configurator,
                CancellationToken ct = default)
                where TResource : Resource
                => new AriaFhirClient<TResource>(configurator, ct);
    }

}
