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
    /// Resolves a practitioner's display name to its corresponding FHIR resource identifier.
    /// </summary>
    public interface IPractitionerResolver
    {
        /// <summary>
        /// Looks up the FHIR Practitioner resource ID for the given display name.
        /// </summary>
        /// <param name="displayName">The human-readable practitioner name to resolve.</param>
        /// <param name="ct">Cancellation token for the async operation.</param>
        /// <returns>
        /// The FHIR resource ID string if found; <see langword="null"/> if no match could be resolved.
        /// </returns>
        Task<string?> ResolveAsync(string displayName, CancellationToken ct);
    }
}
