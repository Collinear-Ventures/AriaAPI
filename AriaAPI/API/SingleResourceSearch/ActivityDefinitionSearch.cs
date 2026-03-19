// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.API.SearchHelpers;
using AriaAPI.Core;
using AriaAPI.Networking.Core;
using AriaAPI.Resources.Includes;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static AriaAPI.API.SearchHelpers.SearchHelpers;
using static AriaAPI.API.SearchHelpers.SearchTypes;

namespace AriaAPI.API.SingleResourceSearch
{
    /// <summary>
    /// Provides search operations for FHIR ActivityDefinition resources using ClientConfigurator and Builder&lt;T&gt;.
    /// </summary>
    public static class ActivityDefinitionSearch
    {
        /// <summary>
        /// Allowed values for the <c>kind</c> parameter, per profile requirements.
        /// </summary>
        private static readonly HashSet<ActivityDefinitionKind> AllowedKinds =
            new()
            {
                ActivityDefinitionKind.Appointment,
                ActivityDefinitionKind.Task
            };

        /// <summary>
        /// Encapsulates search parameters for ActivityDefinition queries.
        /// </summary>
        /// <remarks>
        /// Supported FHIR search parameters used here:
        /// - <c>_id</c> (token): filters by the resource logical id (repeats permitted).
        /// - <c>category</c> (token/string): Aria-defined category (strongly-typed via <see cref="ActivityCategoryCode"/>).
        /// - <c>context-reference</c> (reference/string): department key or reference indicating context.
        /// - <c>kind</c> (token): restricted to <c>Appointment</c> or <c>Task</c>.
        /// - <c>name</c> (string): computationally friendly name of the ActivityDefinition.
        /// - <c>status</c> (token): publication status (e.g., <c>draft</c>, <c>active</c>, <c>retired</c>, <c>unknown</c>).
        ///
        /// Notes:
        /// - Multi-valued parameters (e.g., multiple <c>_id</c> or <c>context-reference</c> values)
        ///   are fanned out into separate queries via <see cref="FanOutSearchHelper"/> and unioned
        ///   client-side (OR semantics). Different parameter types are intersected (AND semantics).
        /// </remarks>
        public sealed class ActivityDefinitionSearchParams
        {
            /// <summary>
            /// One or more logical resource ids to match by _id.
            /// </summary>
            public List<string>? Ids { get; init; }

            /// <summary>
            /// One or more Aria-defined categories.
            /// </summary>
            public ActivityCategoryCode? Category { get; init; }

            /// <summary>
            /// One or more department keys or references for <c>context-reference</c>.
            /// </summary>
            public List<string>? ContextReferences { get; init; }

            /// <summary>
            /// The kind of ActivityDefinition; limited to <c>Appointment</c> or <c>Task</c>.
            /// </summary>
            public ActivityDefinitionKind? Kind { get; init; }

            /// <summary>
            /// Computationally friendly name (string search).
            /// </summary>
            public string? Name { get; init; }

            /// <summary>
            /// Publication status (e.g., Draft, Active, Retired, Unknown).
            /// </summary>
            public PublicationStatus? Status { get; init; }

            /// <summary>
            /// Maximum number of ActivityDefinitions to return. Defaults to <c>SearchExecutor.DefaultServerMaxResults</c> (500) if not specified.
            /// </summary>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;

            /// <summary>
            /// FHIR _include paths for related resources (e.g., Library).
            /// If null or empty, no default includes are applied.
            /// </summary>
            public IEnumerable<ActivityDefinitionInclude>? Includes { get; init; }

            /// <summary>
            /// Apply :iterate modifier to includes if supported by the server.
            /// </summary>
            public bool UseIterateModifier { get; init; } = false;
        }

        /// <summary>
        /// Executes an ActivityDefinition search using the provided parameter bag.
        /// List parameters (Ids, ContextReferences) are fanned out into individual FHIR queries
        /// and aggregated with OR/AND semantics via <see cref="FanOutSearchHelper"/>.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="p">Search parameter bag containing ids, category, context-reference, kind, name, status, includes, and limits.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A deduplicated list of ActivityDefinition resources matching the criteria.</returns>
        public static async Task<List<ActivityDefinition>> SearchActivityDefinitionsAsync(
            ClientConfigurator configurator,
            ActivityDefinitionSearchParams p,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new ActivityDefinitionSearchParams();

            if (p.Kind.HasValue && !AllowedKinds.Contains(p.Kind.Value))
                throw new ArgumentOutOfRangeException(nameof(p.Kind), "Only 'Appointment' or 'Task' kinds are supported.");

            var limit = SearchExecutor.NormalizeLimit(p.ListReturnLimit);

            Builder<ActivityDefinition> MakeBaseBuilder()
            {
                var builder = new Builder<ActivityDefinition>();

                if (p.Category is not null)
                {
                    var token = ActivityCategoryMap.TryGetValue(p.Category.Value, out var s)
                        ? s
                        : p.Category.ToString();
                    builder.With("category", token!);
                }

                if (p.Kind.HasValue)
                    builder.With("kind", ToCode(p.Kind.Value));
                if (!string.IsNullOrWhiteSpace(p.Name))
                    builder.With("name", p.Name!);
                if (p.Status.HasValue)
                    builder.With("status", ToCode(p.Status.Value));

                var modifier = IncludeModifier.None;
                if (p.Includes is not null && p.Includes.Any())
                    builder.Include(p.Includes, modifier: modifier);

                if (limit != int.MaxValue)
                    builder.WithCount(limit);
                return builder;
            }

            var fanOuts = new List<FanOutSearchHelper.FanOutParam>();
            if (p.Ids is { Count: > 0 })
            {
                var values = p.Ids.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                if (values.Count > 0)
                    fanOuts.Add(new FanOutSearchHelper.FanOutParam("_id", values));
            }
            if (p.ContextReferences is { Count: > 0 })
            {
                var values = p.ContextReferences.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                if (values.Count > 0)
                    fanOuts.Add(new FanOutSearchHelper.FanOutParam("context-reference", values));
            }

            return await SearchExecutor.ExecuteAsync(
                configurator,
                MakeBaseBuilder,
                fanOuts,
                limit,
                ct).ConfigureAwait(false);
        }

        // -----------------------------
        // Strongly typed convenience methods
        // -----------------------------

        /// <summary>
        /// Returns ActivityDefinitions that match the specified logical ids.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="ids">Logical ids to match.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<ActivityDefinition>> ByIdsAsync(
            ClientConfigurator configurator,
            IEnumerable<string> ids,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<ActivityDefinitionInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new ActivityDefinitionSearchParams
            {
                Ids = ids?.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList() ?? new(),
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchActivityDefinitionsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns ActivityDefinitions for one or more Aria-defined categories (typed).
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="category">Activity category code.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<ActivityDefinition>> ByCategoriesAsync(
            ClientConfigurator configurator,
            ActivityCategoryCode category,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<ActivityDefinitionInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var cats = category;
            var p = new ActivityDefinitionSearchParams
            {
                Category = cats,
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchActivityDefinitionsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns ActivityDefinitions for the specified context reference(s) (e.g., department key).
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="contextReferences">Context references to filter by.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<ActivityDefinition>> ByContextReferencesAsync(
            ClientConfigurator configurator,
            IEnumerable<string> contextReferences,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<ActivityDefinitionInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new ActivityDefinitionSearchParams
            {
                ContextReferences = contextReferences?.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList() ?? new(),
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchActivityDefinitionsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns ActivityDefinitions matching the specified kind (Appointment or Task).
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="kind">Activity definition kind.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<ActivityDefinition>> ByKindAsync(
            ClientConfigurator configurator,
            ActivityDefinitionKind kind,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<ActivityDefinitionInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            if (!AllowedKinds.Contains(kind))
                throw new ArgumentOutOfRangeException(nameof(kind), "Only 'Appointment' or 'Task' kinds are supported.");

            var p = new ActivityDefinitionSearchParams
            {
                Kind = kind,
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchActivityDefinitionsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns ActivityDefinitions whose computationally friendly name matches the supplied string.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="name">Name to search for.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<ActivityDefinition>> ByNameAsync(
            ClientConfigurator configurator,
            string name,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<ActivityDefinitionInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new ActivityDefinitionSearchParams
            {
                Name = name,
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchActivityDefinitionsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns ActivityDefinitions by publication status (Draft, Active, Retired, Unknown).
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="status">Publication status.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<ActivityDefinition>> ByStatusAsync(
            ClientConfigurator configurator,
            PublicationStatus status,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<ActivityDefinitionInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            var p = new ActivityDefinitionSearchParams
            {
                Status = status,
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchActivityDefinitionsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns ActivityDefinitions filtered by any combination of parameters.
        /// Useful for composing multi-criteria searches in one call.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="ids">Optional logical ids to match.</param>
        /// <param name="category">Optional category filter.</param>
        /// <param name="contextReferences">Optional context references.</param>
        /// <param name="kind">Optional kind filter.</param>
        /// <param name="name">Optional name filter.</param>
        /// <param name="status">Optional status filter.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="includes">Optional includes.</param>
        /// <param name="useIterateModifier">Whether to use the iterate modifier on includes.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<ActivityDefinition>> ByCompositeAsync(
            ClientConfigurator configurator,
            IEnumerable<string>? ids = null,
            ActivityCategoryCode? category = null,
            IEnumerable<string>? contextReferences = null,
            ActivityDefinitionKind? kind = null,
            string? name = null,
            PublicationStatus? status = null,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            IEnumerable<ActivityDefinitionInclude>? includes = null,
            bool useIterateModifier = false,
            CancellationToken ct = default)
        {
            if (kind.HasValue && !AllowedKinds.Contains(kind.Value))
                throw new ArgumentOutOfRangeException(nameof(kind), "Only 'Appointment' or 'Task' kinds are supported.");

            var p = new ActivityDefinitionSearchParams
            {
                Ids = ids?.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList(),
                Category = category,
                ContextReferences = contextReferences?.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList(),
                Kind = kind,
                Name = name,
                Status = status,
                ListReturnLimit = listReturnLimit,
                Includes = includes,
                UseIterateModifier = useIterateModifier
            };
            return SearchActivityDefinitionsAsync(configurator, p, ct);
        }


    }
}
