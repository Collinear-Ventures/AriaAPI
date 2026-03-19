// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using System;
using System.Collections.Generic;
using AriaAPI.Core;
using Microsoft.Extensions.Options;
using Xunit;

namespace AriaAPI.Tests.Core
{
    /// <summary>
    /// Tests for <see cref="FhirClientFactory.GetActiveSystem"/>.
    /// Uses an inline <see cref="IOptionsMonitor{T}"/> stub — no live infrastructure required.
    /// </summary>
    public sealed class FhirClientFactoryTests
    {
        // ── helpers ───────────────────────────────────────────────────────────

        private static FhirClientFactory BuildFactory(FhirOptions options)
            => new FhirClientFactory(new TestOptionsMonitor<FhirOptions>(options));

        private static FhirOptions ValidOptions(
            string activeSystem = "Primary",
            string baseUrl = "https://fhir.example.com",
            string authority = "https://auth.example.com",
            string clientId = "client-id",
            string clientSecret = "secret",
            string scope = "") =>
            new FhirOptions
            {
                ActiveSystem = activeSystem,
                Systems = new Dictionary<string, FhirSystemOptions>
                {
                    [activeSystem] = new FhirSystemOptions
                    {
                        BaseUrl = baseUrl,
                        Auth = new AuthOptions
                        {
                            Authority = authority,
                            ClientId = clientId,
                            ClientSecret = clientSecret,
                            Scope = scope
                        }
                    }
                }
            };

        // ── tests ─────────────────────────────────────────────────────────────

        /// <summary>Happy path: a valid configuration returns the correct FhirSystemOptions.</summary>
        [Fact]
        public void GetActiveSystem_ValidConfig_ReturnsSystem()
        {
            var factory = BuildFactory(ValidOptions());

            var system = factory.GetActiveSystem(out var name);

            Assert.Equal("Primary", name);
            Assert.Equal("https://fhir.example.com", system.BaseUrl);
        }

        /// <summary>An empty ActiveSystem throws InvalidOperationException.</summary>
        [Fact]
        public void GetActiveSystem_EmptyActiveSystem_Throws()
        {
            var options = ValidOptions();
            options.ActiveSystem = "";

            var factory = BuildFactory(options);

            Assert.Throws<InvalidOperationException>(() => factory.GetActiveSystem(out _));
        }

        /// <summary>An ActiveSystem key that is missing from the Systems dictionary throws.</summary>
        [Fact]
        public void GetActiveSystem_SystemKeyMissing_Throws()
        {
            var options = ValidOptions(activeSystem: "Missing");
            // Manually remove the key so the dictionary has no entry for "Missing".
            options.Systems.Clear();

            var factory = BuildFactory(options);

            Assert.Throws<InvalidOperationException>(() => factory.GetActiveSystem(out _));
        }

        /// <summary>An empty BaseUrl throws InvalidOperationException.</summary>
        [Fact]
        public void GetActiveSystem_EmptyBaseUrl_Throws()
        {
            var factory = BuildFactory(ValidOptions(baseUrl: ""));

            Assert.Throws<InvalidOperationException>(() => factory.GetActiveSystem(out _));
        }

        /// <summary>An empty Authority throws InvalidOperationException.</summary>
        [Fact]
        public void GetActiveSystem_EmptyAuthority_Throws()
        {
            var factory = BuildFactory(ValidOptions(authority: ""));

            Assert.Throws<InvalidOperationException>(() => factory.GetActiveSystem(out _));
        }

        /// <summary>An empty ClientId throws InvalidOperationException.</summary>
        [Fact]
        public void GetActiveSystem_EmptyClientId_Throws()
        {
            var factory = BuildFactory(ValidOptions(clientId: ""));

            Assert.Throws<InvalidOperationException>(() => factory.GetActiveSystem(out _));
        }

        /// <summary>An empty ClientSecret throws InvalidOperationException.</summary>
        [Fact]
        public void GetActiveSystem_EmptyClientSecret_Throws()
        {
            var factory = BuildFactory(ValidOptions(clientSecret: ""));

            Assert.Throws<InvalidOperationException>(() => factory.GetActiveSystem(out _));
        }

        /// <summary>An empty Scope does not throw — Scope is validated by FhirService, not FhirClientFactory.</summary>
        [Fact]
        public void GetActiveSystem_ScopeCanBeEmpty_DoesNotThrow()
        {
            var factory = BuildFactory(ValidOptions(scope: ""));

            // Should not throw — Scope validation is the responsibility of FhirService.GetActive().
            var system = factory.GetActiveSystem(out _);
            Assert.NotNull(system);
        }

        // ── FhirService.GetActive() tests ─────────────────────────────────────

        /// <summary>GetActive throws when Scope is an empty string.</summary>
        [Fact]
        public void GetActive_EmptyScope_Throws()
        {
            var service = new FhirService(BuildFactory(ValidOptions(scope: "")));

            Assert.Throws<InvalidOperationException>(() => service.GetActive());
        }

        /// <summary>GetActive returns parsed scopes when Scope is non-empty.</summary>
        [Fact]
        public void GetActive_ValidScope_ReturnsParsedScopes()
        {
            var service = new FhirService(BuildFactory(ValidOptions(scope: "openid fhir")));

            var (_, _, scopes) = service.GetActive();

            Assert.Equal(new[] { "openid", "fhir" }, scopes);
        }

        // ── stub ──────────────────────────────────────────────────────────────

        private sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
        {
            private readonly T _value;
            public TestOptionsMonitor(T value) => _value = value;
            public T CurrentValue => _value;
            public T Get(string? name) => _value;
            public IDisposable? OnChange(Action<T, string?> listener) => null;
        }
    }
}
