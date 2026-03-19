// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using AriaAPI.Core;
using AriaAPI.Networking.Core;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AriaAPI.API.IdentityResolvers
{

    /// <summary>
    /// Resolves a Patient reference ("Patient/{id}") from an MRN using ClientConfigurator + <see cref="Networking.Core.Builder{TResource}"/>.
    /// </summary>
    /// <param name="configurator">FHIR client configurator for Patient queries</param>
    /// <param name="logger">Logger</param>
    public sealed class PatientResolver(ClientConfigurator configurator, ILogger<PatientResolver> logger) : IPatientResolver
    {
        private readonly ClientConfigurator _configurator = configurator ?? throw new ArgumentNullException(nameof(configurator));
        private readonly ILogger<PatientResolver> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        /// <summary>
        /// Returns "Patient/{id}" if found; otherwise null.
        /// </summary>
        public async Task<string?> ResolveAsync(string mrn, CancellationToken ct)
        {
            mrn = mrn.Trim();
            if (string.IsNullOrWhiteSpace(mrn)) return null;
            ct.ThrowIfCancellationRequested();

            var patientClient = _configurator.ForResource<Patient>(ct);
            var builder = new Builder<Patient>();

            // Prefer exact token match across any identifier system: identifier=|MRN
            // FHIR token search "system|value" supports empty system with a leading pipe
            builder.With("identifier", $"|{mrn}");

            // Optional: ask for active patients first if server supports Patient.active search
            // builder.With("active", "true");

            builder.WithCount(10); // small page, we’ll pick best match
            var searchParams = builder.Build();
            var candidates = await patientClient.AggregateResourcesAsync(searchParams).ConfigureAwait(false);

            if (candidates is null || candidates.Count == 0)
            {
                _logger.LogWarning("No Patient found for MRN-hash={MrnHash}.", PhiMask.Mask(mrn));
                return null;
            }

            // Prefer active, then exact identifier value, then first
            var best = candidates
                .OrderByDescending(p => p.Active == true)
                .ThenByDescending(p => HasIdentifierValue(p, mrn))
                .FirstOrDefault();

            _logger.LogDebug("Resolved MRN-hash={MrnHash} -> Patient/{Id}", PhiMask.Mask(mrn), best!.Id);

            if (best is null || string.IsNullOrWhiteSpace(best.Id))
            {
                _logger.LogWarning("Patient candidates returned but none with a usable Id for MRN-hash={MrnHash}.", PhiMask.Mask(mrn));
                return null;
            }

            return $"Patient/{best.Id}";
        }

        private static bool HasIdentifierValue(Patient p, string value)
        {
            if (p.Identifier == null) return false;
            return p.Identifier.Any(id =>
                string.Equals(id?.Value, value, StringComparison.OrdinalIgnoreCase));
        }
    }
}