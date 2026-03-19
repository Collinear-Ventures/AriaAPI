// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using AriaAPI.Core;
using AriaAPI.Networking.Core;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AriaAPI.API.IdentityResolvers
{

    /// <summary>
    /// Resolves a Practitioner reference ("Practitioner/{id}") from a display name.
    /// </summary>
    public sealed class PractitionerResolver : IPractitionerResolver
    {
        private readonly ClientConfigurator _configurator;
        private readonly ILogger<PractitionerResolver> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PractitionerResolver"/> class.
        /// </summary>
        /// <param name="configurator">The client configurator providing the authenticated FHIR client.</param>
        /// <param name="logger">The logger for diagnostic messages.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configurator"/> or <paramref name="logger"/> is <see langword="null"/>.</exception>
        public PractitionerResolver(ClientConfigurator configurator, ILogger<PractitionerResolver> logger)
        {
            _configurator = configurator ?? throw new ArgumentNullException(nameof(configurator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Returns "Practitioner/{id}" if found; otherwise null.
        /// </summary>
        public async Task<string?> ResolveAsync(string displayName, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(displayName)) return null;
            ct.ThrowIfCancellationRequested();

            var practClient = _configurator.ForResource<Practitioner>(ct);
            var builder = new Builder<Practitioner>();

            // Basic name search. If you also have an identifier (e.g., NPI), add:
            // builder.With("identifier", $"|{npi}");
            builder.With("name", displayName);

            builder.WithCount(20);
            var searchParams = builder.Build();
            var candidates = await practClient.AggregateResourcesAsync(searchParams).ConfigureAwait(false);

            if (candidates is null || candidates.Count == 0)
            {
                _logger.LogWarning("No Practitioner found for Name-hash={NameHash}.", PhiMask.Mask(displayName));
                return null;
            }

            var normalizedTarget = Normalize(displayName);

            // Prefer active, then exact normalized match on any official name, then first
            var best = candidates
                .OrderByDescending(p => p.Active == true)
                .ThenByDescending(p => NamesContain(p, normalizedTarget))
                .FirstOrDefault();

            if (best is null || string.IsNullOrWhiteSpace(best.Id))
            {
                _logger.LogWarning("Practitioner candidates returned but none with a usable Id for Name-hash={NameHash}.", PhiMask.Mask(displayName));
                return null;
            }

            return $"Practitioner/{best.Id}";
        }

        private static bool NamesContain(Practitioner p, string normalizedTarget)
        {
            // Check name.text and family+given composites
            if (p.Name == null) return false;

            foreach (var hn in p.Name)
            {
                var text = Normalize(hn.Text);
                if (!string.IsNullOrEmpty(text) && text.Equals(normalizedTarget, StringComparison.Ordinal)) return true;

                var composite = Normalize($"{hn.Family} {string.Join(" ", hn.Given ?? Array.Empty<string>())}");
                if (!string.IsNullOrWhiteSpace(composite) && composite.Equals(normalizedTarget, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static string Normalize(string? s)
        {
            s ??= string.Empty;
            s = s.Trim();
            s = s.Replace(",", " ").Replace(".", " ").Replace("  ", " ");
            s = s.ToUpperInvariant();
            return s;
        }
    }
}