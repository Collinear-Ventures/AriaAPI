// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using AriaAPI.Core;
using Hl7.Fhir.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AriaAPI.Networking.Core
{
    internal class AriaClientFactory
    {

        /// <summary>
        /// Creates a generic AriaFhirClient for the specified FHIR resource type.
        /// </summary>
        /// <typeparam name="TResource">The FHIR resource type (e.g., Patient, DocumentReference).</typeparam>
        /// <param name="configurator">The shared ClientConfigurator instance.</param>
        /// <returns>An AriaFhirClient for the specified resource type.</returns>
        public static AriaFhirClient<TResource> Create<TResource>(ClientConfigurator configurator)
            where TResource : Resource
        {
            return new AriaFhirClient<TResource>(configurator);
        }

    }
}
