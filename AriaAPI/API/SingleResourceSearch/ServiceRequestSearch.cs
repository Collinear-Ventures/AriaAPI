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
using static AriaAPI.API.SearchHelpers.SearchTypes;

namespace AriaAPI.API.SingleResourceSearch
{
    /// <summary>
    /// Provides search operations for FHIR <see cref="ServiceRequest"/> resources using
    /// <see cref="ClientConfigurator"/> and <c>Builder&lt;T&gt;</c>.
    /// </summary>
    /// <remarks>
    /// Supported search parameters:
    /// <list type="table">
    ///   <listheader>
    ///     <term>Parameter</term>
    ///     <description>Description</description>
    ///   </listheader>
    ///   <item><term><c>_id</c></term><description>Logical ID of the resource.</description></item>
    ///   <item><term><c>category</c></term><description>ServiceRequest category code (token).</description></item>
    ///   <item><term><c>code</c></term><description>ServiceRequest code (token).</description></item>
    ///   <item><term><c>intent</c></term><description>Intent of the request (<c>original-order</c> | <c>filler-order</c>).</description></item>
    ///   <item><term><c>patient</c></term><description>Subject of the request is a patient (Reference or logical id token).</description></item>
    ///   <item><term><c>subject</c></term><description>Who/what the request is about (Reference).</description></item>
    /// </list>
    ///
    /// Includes:
    /// <list type="bullet">
    ///   <item><description><c>_include=ServiceRequest:based-on</c></description></item>
    ///   <item><description><c>_include=ServiceRequest:patient</c></description></item>
    ///   <item><description><c>_include=ServiceRequest:requester</c></description></item>
    /// </list>
    /// </remarks>
    public static class ServiceRequestSearch
    {
        /// <summary>
        /// Encapsulates search parameters for ServiceRequest queries.
        /// All parameters are optional; supply any combination to constrain results.
        /// </summary>
        public sealed class ServiceRequestSearchParams
        {
            /// <summary>Logical ID of the ServiceRequest resource.</summary>
            public string? Id { get; init; }

            /// <summary>ServiceRequest category code (token).</summary>
            public string? Category { get; init; }

            /// <summary>ServiceRequest code (token).</summary>
            public string? Code { get; init; }

            /// <summary>
            /// Intent (enum). If set, takes precedence over <see cref="IntentToken"/>.
            /// </summary>
            public ServiceRequestIntentShort? Intent { get; init; }

            /// <summary>
            /// Intent (free-text/system token). Used if <see cref="Intent"/> is not set.
            /// </summary>
            public string? IntentToken { get; init; }

            /// <summary>Patient reference or id.</summary>
            public string? Patient { get; init; }

            /// <summary>Subject reference.</summary>
            public string? Subject { get; init; }

            /// <summary>Include <c>ServiceRequest:based-on</c>.</summary>
            public bool IncludeBasedOn { get; init; } = false;

            /// <summary>Include <c>ServiceRequest:patient</c>.</summary>
            public bool IncludePatient { get; init; } = false;

            /// <summary>Include <c>ServiceRequest:requester</c>.</summary>
            public bool IncludeRequester { get; init; } = false;

            /// <summary>Client-side defensive cap (applied after aggregation). Defaults to <c>SearchExecutor.DefaultServerMaxResults</c> (500).</summary>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;
        }

        /// <summary>
        /// Executes a ServiceRequest search using the provided parameter bag.
        /// Builds FHIR <see cref="SearchParams"/> via <c>Builder&lt;T&gt;</c> and aggregates results across pages.
        /// Also performs client-side enrichment for <c>based-on</c>, <c>patient</c>, and <c>requester</c>
        /// when server-side <c>_include</c> is unsupported or not populated.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="p">Search parameter bag.</param>
        /// <param name="ct">Cancellation token.</param>
        public static async Task<List<ServiceRequest>> SearchServiceRequestsAsync(
            ClientConfigurator configurator,
            ServiceRequestSearchParams p,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new ServiceRequestSearchParams();

            var limit = SearchExecutor.NormalizeLimit(p.ListReturnLimit);

            var results = await SearchExecutor.ExecuteAsync(
                configurator,
                () =>
                {
                    var builder = new Builder<ServiceRequest>();

                    // -------- Filters --------
                    if (!string.IsNullOrWhiteSpace(p.Id))
                        builder.With("_id", p.Id);

                    if (!string.IsNullOrWhiteSpace(p.Category))
                        builder.With("category", p.Category);

                    if (!string.IsNullOrWhiteSpace(p.Code))
                        builder.With("code", p.Code);

                    // intent can be enum or token; enum takes precedence
                    var intentToken = ResolveIntentToken(p.Intent, p.IntentToken);
                    if (!string.IsNullOrWhiteSpace(intentToken))
                        builder.With("intent", intentToken);

                    // patient (alias of subject on many servers)
                    if (!string.IsNullOrWhiteSpace(p.Patient))
                        builder.With("patient", p.Patient);

                    if (!string.IsNullOrWhiteSpace(p.Subject))
                        builder.With("subject", p.Subject);

                    if (limit > 0 && limit != int.MaxValue)
                        builder.WithCount(limit);

                    // _includes are commented out (server does not support them)
                    //if (p.IncludeBasedOn)
                    //    builder.Include(ServiceRequestInclude.BasedOn);
                    //if (p.IncludePatient)
                    //    builder.Include(ServiceRequestInclude.Patient);
                    //if (p.IncludeRequester)
                    //    builder.Include(ServiceRequestInclude.Requester);

                    return builder;
                },
                null,
                limit,
                ct).ConfigureAwait(false);

            // -------- Enrichment (client-side) --------
            // If _include wasn't honored or not supported, try to fetch references and attach them to ServiceRequest.Contained
            if (p.IncludePatient || p.IncludeBasedOn || p.IncludeRequester)
            {
                var patientClient = configurator.ForResource<Patient>(ct);
                var carePlanClient = configurator.ForResource<CarePlan>(ct);
                var srClient = configurator.ForResource<ServiceRequest>(ct);
                var practitionerClient = configurator.ForResource<Practitioner>(ct);
                var organizationClient = configurator.ForResource<Organization>(ct);

                foreach (var sr in results)
                {
                    sr.Contained ??= new List<Resource>();

                    // ---- Patient enrichment (from subject) ----
                    if (p.IncludePatient)
                    {
                        var hasPatientContained = sr.Contained.OfType<Patient>().Any();
                        if (!hasPatientContained)
                        {
                            var subjectRefStr = sr.Subject?.Reference;
                            if (!string.IsNullOrWhiteSpace(subjectRefStr))
                            {
                                try
                                {
                                    var patient = await patientClient.ReadAsync(subjectRefStr).ConfigureAwait(false);
                                    if (patient != null) sr.Contained.Add(patient);
                                }
                                catch { /* best-effort */ }
                            }
                        }
                    }

                    // ---- BasedOn enrichment (CarePlan or another ServiceRequest commonly) ----
                    if (p.IncludeBasedOn && sr.BasedOn != null && sr.BasedOn.Count > 0)
                    {
                        var containedKeys = sr.Contained.Select(r => $"{r.TypeName}/{r.Id}").ToHashSet(StringComparer.OrdinalIgnoreCase);
                        foreach (var based in sr.BasedOn)
                        {
                            var refStr = based?.Reference;
                            if (string.IsNullOrWhiteSpace(refStr)) continue;

                            if (containedKeys.Contains(refStr)) continue;

                            try
                            {
                                Resource? fetched = null;

                                if (IsTypeRef(refStr, "CarePlan"))
                                    fetched = await carePlanClient.ReadAsync(refStr).ConfigureAwait(false);
                                else if (IsTypeRef(refStr, "ServiceRequest"))
                                    fetched = await srClient.ReadAsync(refStr).ConfigureAwait(false);
                                else
                                    fetched = null; // extend here if your server uses other basedOn targets

                                if (fetched != null)
                                {
                                    sr.Contained.Add(fetched);
                                    containedKeys.Add($"{fetched.TypeName}/{fetched.Id}");
                                }
                            }
                            catch { /* best-effort */ }
                        }
                    }

                    // ---- Requester enrichment ----
                    if (p.IncludeRequester)
                    {
                        // If the server didn't include requester, look in extensions or use performer as a pragmatic fallback
                        var hasRequester = sr.Contained.OfType<Practitioner>().Any() || sr.Contained.OfType<Organization>().Any();
                        if (!hasRequester)
                        {
                            var requesterRef = GetRequesterReference(sr);
                            var refStr = requesterRef?.Reference;

                            if (!string.IsNullOrWhiteSpace(refStr))
                            {
                                try
                                {
                                    Resource? fetched = null;

                                    if (IsTypeRef(refStr, "Practitioner"))
                                        fetched = await practitionerClient.ReadAsync(refStr).ConfigureAwait(false);
                                    else if (IsTypeRef(refStr, "Organization"))
                                        fetched = await organizationClient.ReadAsync(refStr).ConfigureAwait(false);
                                    else
                                        fetched = null; // extend if requester can be Patient/RelatedPerson in your implementation

                                    if (fetched != null)
                                        sr.Contained.Add(fetched);
                                }
                                catch { /* best-effort */ }
                            }
                        }
                    }
                }
            }

            return results;
        }

        private static string? ResolveIntentToken(ServiceRequestIntentShort? intentEnum, string? intentToken)
        {
            if (intentEnum.HasValue) return IntentToToken(intentEnum.Value);
            return string.IsNullOrWhiteSpace(intentToken) ? null : intentToken;
        }

        /// <summary>
        /// Attempts to identify a requester reference from extensions or falls back to performer.
        /// R4 does not have a native 'requester' property; many servers expose it via extension.
        /// </summary>
        private static ResourceReference? GetRequesterReference(ServiceRequest sr)
        {
            // Look for an extension whose URL indicates requester and whose value is a Reference
            var extRequester = sr.Extension?
                .FirstOrDefault(e =>
                    e.Url != null &&
                    e.Url.IndexOf("requester", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    e.Value is ResourceReference);

            if (extRequester?.Value is ResourceReference rrExt)
                return rrExt;

            // Pragmatic fallback: take the first performer as "requester" if applicable to your workflow
            var perf = sr.Performer?.FirstOrDefault();
            if (perf != null) return perf;

            return null;
        }

        private static bool IsTypeRef(string reference, string type)
        {
            if (string.IsNullOrWhiteSpace(reference) || string.IsNullOrWhiteSpace(type)) return false;
            // Works for absolute or relative references
            // Examples: "CarePlan/123", "https://server/Organization/789"
            return reference.Split('/').Any(seg => seg.Equals(type, StringComparison.OrdinalIgnoreCase));
        }

        // -----------------------------
        // Strongly-typed convenience methods
        // -----------------------------

        /// <summary>
        /// Returns ServiceRequests by resource ID.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="id">Logical ID of the resource.</param>
        /// <param name="includeBasedOn">Whether to include based-on references.</param>
        /// <param name="includePatient">Whether to include patient references.</param>
        /// <param name="includeRequester">Whether to include requester references.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<ServiceRequest>> ByIdAsync(
            ClientConfigurator configurator,
            string id,
            bool includeBasedOn = false,
            bool includePatient = false,
            bool includeRequester = false,
            int listReturnLimit = int.MaxValue,
            CancellationToken ct = default)
        {
            var p = new ServiceRequestSearchParams
            {
                Id = id,
                IncludeBasedOn = includeBasedOn,
                IncludePatient = includePatient,
                IncludeRequester = includeRequester,
                ListReturnLimit = listReturnLimit
            };
            return SearchServiceRequestsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns ServiceRequests by patient reference.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="patientRefOrId">Patient reference or id.</param>
        /// <param name="includeBasedOn">Whether to include based-on references.</param>
        /// <param name="includePatient">Whether to include patient references.</param>
        /// <param name="includeRequester">Whether to include requester references.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<ServiceRequest>> ByPatientAsync(
            ClientConfigurator configurator,
            string patientRefOrId,
            bool includeBasedOn = false,
            bool includePatient = false,
            bool includeRequester = false,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new ServiceRequestSearchParams
            {
                Patient = patientRefOrId,
                IncludeBasedOn = includeBasedOn,
                IncludePatient = includePatient,
                IncludeRequester = includeRequester,
                ListReturnLimit = listReturnLimit
            };
            return SearchServiceRequestsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns ServiceRequests by code.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="codeToken">Code token to filter by.</param>
        /// <param name="intent">Optional intent filter.</param>
        /// <param name="includeBasedOn">Whether to include based-on references.</param>
        /// <param name="includePatient">Whether to include patient references.</param>
        /// <param name="includeRequester">Whether to include requester references.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<ServiceRequest>> ByCodeAsync(
            ClientConfigurator configurator,
            string codeToken,
            ServiceRequestIntentShort? intent = null,
            bool includeBasedOn = false,
            bool includePatient = false,
            bool includeRequester = false,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new ServiceRequestSearchParams
            {
                Code = codeToken,
                Intent = intent,
                IncludeBasedOn = includeBasedOn,
                IncludePatient = includePatient,
                IncludeRequester = includeRequester,
                ListReturnLimit = listReturnLimit
            };
            return SearchServiceRequestsAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns ServiceRequests by intent.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="intent">Intent to filter by.</param>
        /// <param name="includeBasedOn">Whether to include based-on references.</param>
        /// <param name="includePatient">Whether to include patient references.</param>
        /// <param name="includeRequester">Whether to include requester references.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<ServiceRequest>> ByIntentAsync(
            ClientConfigurator configurator,
            ServiceRequestIntentShort intent,
            bool includeBasedOn = false,
            bool includePatient = false,
            bool includeRequester = false,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            var p = new ServiceRequestSearchParams
            {
                Intent = intent,
                IncludeBasedOn = includeBasedOn,
                IncludePatient = includePatient,
                IncludeRequester = includeRequester,
                ListReturnLimit = listReturnLimit
            };
            return SearchServiceRequestsAsync(configurator, p, ct);
        }
    }
}
