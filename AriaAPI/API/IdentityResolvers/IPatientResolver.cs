// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AriaAPI.API.IdentityResolvers
{
    /// <summary>
    /// Optional resolver contracts (wire these to your existing search or identity services).
    /// </summary>
    public interface IPatientResolver
    {
        /// <summary>
        /// Resolves a patient FHIR reference (e.g., "Patient/123") from the given MRN.
        /// </summary>
        /// <param name="mrn">The medical record number to look up.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The patient FHIR reference string if found; otherwise <see langword="null"/>.</returns>
        Task<string?> ResolveAsync(string mrn, CancellationToken ct);
    }
}
