// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.API.SearchHelpers;
using AriaAPI.Core;
using AriaAPI.Networking.Core;
using AriaAPI.Resources.Includes;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static AriaAPI.API.SearchHelpers.SearchHelpers;
using static AriaAPI.API.SearchHelpers.SearchTypes;

namespace AriaAPI.API.SingleResourceSearch
{
    /// <summary>
    /// Provides search operations for FHIR AllergyIntolerance resources using ClientConfigurator and Builder&lt;T&gt;.
    /// </summary>
    public static class AllergyIntoleranceSearch
    {
        /// <summary>
        /// Encapsulates search parameters for AllergyIntolerance queries.
        /// </summary>
        public sealed class AllergyIntoleranceSearchParams
        {
            /// <summary>
            /// The logical id of the resource (FHIR <c>_id</c> search parameter).
            /// </summary>
            public string? Id { get; init; }

            /// <summary>
            /// One or more categories to filter by (FHIR <c>category</c>: food | medication).
            /// Supports multiple values (FHIR repeats).
            /// </summary>
            public List<AllergyCategory>? Categories { get; init; }

            /// <summary>
            /// One or more coded identifiers for the allergy/intolerance concept (FHIR <c>code</c> token).
            /// Accepts code-only or <c>system|code</c> form.
            /// </summary>
            public List<string>? Codes { get; init; }

            /// <summary>
            /// One or more external identifiers for this record (FHIR <c>identifier</c> token).
            /// Accepts value-only or <c>system|value</c> form.
            /// </summary>
            public List<string>? Identifiers { get; init; }

            /// <summary>
            /// Inclusive start of onset date/time window (FHIR date filter on <c>onset-date</c>: <c>ge</c>).
            /// </summary>
            public DateTimeOffset? OnsetStart { get; init; }

            /// <summary>
            /// Inclusive end of onset date/time window (FHIR date filter on <c>onset-date</c>: <c>le</c>).
            /// </summary>
            public DateTimeOffset? OnsetEnd { get; init; }

            /// <summary>
            /// Patient reference (id or absolute/relative reference) for whom the allergy is recorded (FHIR <c>patient</c>).
            /// </summary>
            public string? Patient { get; init; }

            /// <summary>
            /// One or more verification statuses (FHIR <c>verification-status</c>: confirmed | unconfirmed | entered-in-error).
            /// Supports multiple values (FHIR repeats).
            /// </summary>
            public List<AllergyVerificationStatus>? VerificationStatuses { get; init; }

            /// <summary>
            /// Maximum number of resources to return. Defaults to <see cref="SearchExecutor.DefaultServerMaxResults"/> (500) if not specified.
            /// </summary>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;

            /// <summary>
            /// FHIR _include paths for related resources.
            /// If null or empty, defaults to including <see cref="AllergyIntoleranceInclude.Patient"/>.
            /// </summary>
            public IEnumerable<AllergyIntoleranceInclude>? Includes { get; init; }

            /// <summary>
            /// Apply <c>:iterate</c> modifier to includes if supported by the server.
            /// </summary>
            public bool UseIterateModifier { get; init; } = false;
        }

        // -----------------------------
        // Core Search
        // -----------------------------

        /// <summary>
        /// Executes an AllergyIntolerance search using the provided parameter bag.
        /// List parameters (Categories, Codes, Identifiers, VerificationStatuses) are fanned out
        /// into individual FHIR queries and aggregated with OR/AND semantics via
        /// <see cref="FanOutSearchHelper"/>.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="p">Search parameter bag containing id, category, code, identifier, onset window, patient, verification-status, includes, and limits.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A deduplicated list of AllergyIntolerance resources matching the criteria.</returns>
        public static async Task<List<AllergyIntolerance>> SearchAllergiesAsync(
            ClientConfigurator configurator,
            AllergyIntoleranceSearchParams p,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new AllergyIntoleranceSearchParams();

            var limit = SearchExecutor.NormalizeLimit(p.ListReturnLimit);

            Builder<AllergyIntolerance> MakeBaseBuilder()
            {
                var builder = new Builder<AllergyIntolerance>();

                if (!string.IsNullOrWhiteSpace(p.Id))
                    builder.With("_id", p.Id);

                if (p.OnsetStart.HasValue)
                    builder.With("onset-date", $"ge{p.OnsetStart.Value:O}");
                if (p.OnsetEnd.HasValue)
                    builder.With("onset-date", $"le{p.OnsetEnd.Value:O}");

                if (!string.IsNullOrWhiteSpace(p.Patient))
                    builder.With("patient", p.Patient);

                var modifier = IncludeModifier.None;
                if (p.Includes is not null && p.Includes.Any())
                    builder.Include(p.Includes, modifier: modifier);
                else
                    builder.Include(AllergyIntoleranceInclude.Patient);

                if (limit > 0 && limit < int.MaxValue)
                    builder.WithCount(limit);

                return builder;
            }

            var fanOuts = new List<FanOutSearchHelper.FanOutParam>();
            if (p.Categories is { Count: > 0 })
                fanOuts.Add(new FanOutSearchHelper.FanOutParam("category",
                    p.Categories.Select(cat => CategoryToParam(cat)).ToList()));
            if (p.Codes is { Count: > 0 })
            {
                var values = p.Codes.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                if (values.Count > 0)
                    fanOuts.Add(new FanOutSearchHelper.FanOutParam("code", values));
            }
            if (p.Identifiers is { Count: > 0 })
            {
                var values = p.Identifiers.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                if (values.Count > 0)
                    fanOuts.Add(new FanOutSearchHelper.FanOutParam("identifier", values));
            }
            if (p.VerificationStatuses is { Count: > 0 })
                fanOuts.Add(new FanOutSearchHelper.FanOutParam("verification-status",
                    p.VerificationStatuses.Select(vs => VerificationStatusToParam(vs)).ToList()));

            return await SearchExecutor.ExecuteAsync(
                configurator,
                MakeBaseBuilder,
                fanOuts,
                limit,
                ct).ConfigureAwait(false);
        }

        // -----------------------------
        // Convenience Methods
        // -----------------------------

        /// <summary>
        /// Returns allergies for a specific patient id/reference.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="patient">Patient reference or id.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<AllergyIntolerance>> ByPatientAsync(
            ClientConfigurator configurator,
            string patient,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<AllergyIntoleranceInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new AllergyIntoleranceSearchParams
            {
                Patient = patient,
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchAllergiesAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns allergies for a patient within an onset date/time window.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="patient">Patient reference or id.</param>
        /// <param name="onsetStart">Inclusive onset start date.</param>
        /// <param name="onsetEnd">Inclusive onset end date.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<AllergyIntolerance>> ByPatientAndOnsetAsync(
            ClientConfigurator configurator,
            string patient,
            DateTimeOffset onsetStart,
            DateTimeOffset onsetEnd,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<AllergyIntoleranceInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new AllergyIntoleranceSearchParams
            {
                Patient = patient,
                OnsetStart = onsetStart,
                OnsetEnd = onsetEnd,
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchAllergiesAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns allergies by one or more categories.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="categories">Categories to filter by.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<AllergyIntolerance>> ByCategoriesAsync(
            ClientConfigurator configurator,
            IEnumerable<AllergyCategory> categories,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<AllergyIntoleranceInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new AllergyIntoleranceSearchParams
            {
                Categories = categories?.ToList() ?? new List<AllergyCategory>(),
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchAllergiesAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns allergies by code(s) (token search).
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="codes">Code tokens to filter by.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<AllergyIntolerance>> ByCodesAsync(
            ClientConfigurator configurator,
            IEnumerable<string> codes,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<AllergyIntoleranceInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new AllergyIntoleranceSearchParams
            {
                Codes = codes?.Where(s => !string.IsNullOrWhiteSpace(s)).ToList() ?? new(),
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchAllergiesAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns allergies by verification status(es).
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="statuses">Verification statuses to filter by.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<AllergyIntolerance>> ByVerificationStatusAsync(
            ClientConfigurator configurator,
            IEnumerable<AllergyVerificationStatus> statuses,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<AllergyIntoleranceInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new AllergyIntoleranceSearchParams
            {
                VerificationStatuses = statuses?.ToList() ?? new(),
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchAllergiesAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns a single AllergyIntolerance by its logical id.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="id">Logical id of the resource.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<AllergyIntolerance>> ByIdAsync(
            ClientConfigurator configurator,
            string id,
            IEnumerable<AllergyIntoleranceInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new AllergyIntoleranceSearchParams
            {
                Id = id,
                ListReturnLimit = int.MaxValue,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchAllergiesAsync(configurator, p, ct);
        }

    }
}
