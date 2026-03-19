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
using Task = Hl7.Fhir.Model.Task;

namespace AriaAPI.API.SingleResourceSearch
{
    /// <summary>
    /// Provides search operations for FHIR <see cref="Task"/> resources using
    /// <see cref="ClientConfigurator"/> and <c>Builder&lt;T&gt;</c>.
    /// </summary>
    /// <remarks>
    /// Supported search parameters:
    /// <list type="table">
    ///   <listheader>
    ///     <term>Parameter</term>
    ///     <description>Description</description>
    ///   </listheader>
    ///   <item><term><c>_has</c></term><description>Reverse chaining parameter for related resources.</description></item>
    ///   <item><term><c>_id</c></term><description>Logical ID of the Task resource.</description></item>
    ///   <item><term><c>based-on</c></term><description>Reference to the plan or request fulfilled by the Task.</description></item>
    ///   <item><term><c>code</c></term><description>Task code (token).</description></item>
    ///   <item><term><c>focus</c></term><description>Focus of the Task (Reference).</description></item>
    ///   <item><term><c>identifier</c></term><description>Business identifier for the Task.</description></item>
    ///   <item><term><c>modified</c></term><description>Last modified date/time (supports prefixes/ranges).</description></item>
    ///   <item><term><c>owner</c></term><description>Owner responsible for the Task.</description></item>
    ///   <item><term><c>patient</c></term><description>Patient associated with the Task.</description></item>
    ///   <item><term><c>period</c></term><description>Period during which the Task is active.</description></item>
    ///   <item><term><c>recipient</c></term><description>Recipient of the Task.</description></item>
    ///   <item><term><c>status</c></term><description>Status of the Task (e.g., requested, in-progress, completed).</description></item>
    ///   <item><term><c>subject</c></term><description>Subject of the Task.</description></item>
    /// </list>
    ///
    /// Includes:
    /// <list type="bullet">
    ///   <item><description><c>_include=Task:based-on</c></description></item>
    ///   <item><description><c>_include=Task:for:recipient</c></description></item>
    /// </list>
    /// </remarks>
    public static class TaskSearch
    {
        /// <summary>Encapsulates search parameters for Task queries.</summary>
        public sealed class TaskSearchParams
        {
            /// <summary>Reverse chaining parameter.</summary>
            public string? Has { get; init; }

            /// <summary>Logical ID of the Task resource.</summary>
            public string? Id { get; init; }

            /// <summary>Reference to the plan or request fulfilled by the Task.</summary>
            public string? BasedOn { get; init; }

            /// <summary>Task code (token).</summary>
            public string? Code { get; init; }

            /// <summary>Focus of the Task (Reference).</summary>
            public string? Focus { get; init; }

            /// <summary>Business identifier for the Task.</summary>
            public string? Identifier { get; init; }

            /// <summary>Last modified date/time.</summary>
            public string? Modified { get; init; }

            /// <summary>Owner responsible for the Task.</summary>
            public string? Owner { get; init; }

            /// <summary>Patient associated with the Task.</summary>
            public string? Patient { get; init; }

            /// <summary>Period during which the Task is active.</summary>
            public string? Period { get; init; }

            /// <summary>Recipient of the Task.</summary>
            public string? Recipient { get; init; }

            /// <summary>Status of the Task.</summary>
            public string? Status { get; init; }

            /// <summary>Subject of the Task.</summary>
            public string? Subject { get; init; }

            /// <summary>Whether to include based-on references.</summary>
            public bool IncludeBasedOn { get; init; } = false;

            /// <summary>Whether to include recipient references.</summary>
            public bool IncludeRecipient { get; init; } = false;

            /// <summary>Client-side defensive cap. Defaults to <c>SearchExecutor.DefaultServerMaxResults</c> (500).</summary>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;
        }

        /// <summary>
        /// Executes a Task search using the provided parameter bag.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="p">Search parameter bag.</param>
        /// <param name="ct">Cancellation token.</param>
        public static async Task<List<Task>> SearchTasksAsync(
            ClientConfigurator configurator,
            TaskSearchParams p,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new TaskSearchParams();

            var limit = SearchExecutor.NormalizeLimit(p.ListReturnLimit);

            var results = await SearchExecutor.ExecuteAsync(
                configurator,
                () =>
                {
                    var builder = new Builder<Task>();

                    if (!string.IsNullOrWhiteSpace(p.Has)) builder.With("_has", p.Has);
                    if (!string.IsNullOrWhiteSpace(p.Id)) builder.With("_id", p.Id);
                    if (!string.IsNullOrWhiteSpace(p.BasedOn)) builder.With("based-on", p.BasedOn);
                    if (!string.IsNullOrWhiteSpace(p.Code)) builder.With("code", p.Code);
                    if (!string.IsNullOrWhiteSpace(p.Focus)) builder.With("focus", p.Focus);
                    if (!string.IsNullOrWhiteSpace(p.Identifier)) builder.With("identifier", p.Identifier);
                    if (!string.IsNullOrWhiteSpace(p.Modified)) builder.With("modified", p.Modified);
                    if (!string.IsNullOrWhiteSpace(p.Owner)) builder.With("owner", p.Owner);
                    if (!string.IsNullOrWhiteSpace(p.Patient)) builder.With("patient", p.Patient);
                    if (!string.IsNullOrWhiteSpace(p.Period)) builder.With("period", p.Period);
                    if (!string.IsNullOrWhiteSpace(p.Recipient)) builder.With("recipient", p.Recipient);
                    if (!string.IsNullOrWhiteSpace(p.Status)) builder.With("status", p.Status);
                    if (!string.IsNullOrWhiteSpace(p.Subject)) builder.With("subject", p.Subject);

                    if (limit > 0 && limit != int.MaxValue)
                        builder.WithCount(limit);

                    if (p.IncludeBasedOn) builder.Include(TaskInclude.BasedOn);
                    if (p.IncludeRecipient) builder.Include(TaskInclude.Recipient);

                    return builder;
                },
                null,
                limit,
                ct).ConfigureAwait(false);

            // -------- Enrichment --------
            if (p.IncludeBasedOn || p.IncludeRecipient)
            {
                var srClient = configurator.ForResource<ServiceRequest>(ct);
                var practitionerClient = configurator.ForResource<Practitioner>(ct);
                var orgClient = configurator.ForResource<Organization>(ct);

                foreach (var task in results)
                {
                    task.Contained ??= new List<Resource>();

                    // BasedOn enrichment
                    if (p.IncludeBasedOn && task.BasedOn != null)
                    {
                        foreach (var based in task.BasedOn)
                        {
                            var refStr = based.Reference;
                            if (string.IsNullOrWhiteSpace(refStr)) continue;

                            try
                            {
                                if (refStr.StartsWith("ServiceRequest/", StringComparison.OrdinalIgnoreCase))
                                {
                                    var sr = await srClient.ReadAsync(refStr).ConfigureAwait(false);
                                    if (sr != null) task.Contained.Add(sr);
                                }
                            }
                            catch { /* best-effort */ }
                        }
                    }

                    // Recipient enrichment
                    if (p.IncludeRecipient && task.Requester != null)
                    {

                        var refStr = task.Requester.Reference;
                        if (string.IsNullOrWhiteSpace(refStr)) continue;

                        try
                        {
                            Resource? fetched = null;
                            if (refStr.StartsWith("Practitioner/", StringComparison.OrdinalIgnoreCase))
                                fetched = await practitionerClient.ReadAsync(refStr).ConfigureAwait(false);
                            else if (refStr.StartsWith("Organization/", StringComparison.OrdinalIgnoreCase))
                                fetched = await orgClient.ReadAsync(refStr).ConfigureAwait(false);

                            if (fetched != null) task.Contained.Add(fetched);
                        }
                        catch { /* ignore errors */ }

                    }
                }
            }

            return results;
        }

        // Convenience methods

        /// <summary>
        /// Returns Tasks by resource ID.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="id">Logical ID of the resource.</param>
        /// <param name="includeBasedOn">Whether to include based-on references.</param>
        /// <param name="includeRecipient">Whether to include recipient references.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Task>> ByIdAsync(ClientConfigurator configurator, string id,
            bool includeBasedOn = false, bool includeRecipient = false, int listReturnLimit = int.MaxValue, CancellationToken ct = default)
        {
            var p = new TaskSearchParams
            {
                Id = id,
                IncludeBasedOn = includeBasedOn,
                IncludeRecipient = includeRecipient,
                ListReturnLimit = listReturnLimit
            };
            return SearchTasksAsync(configurator, p, ct);
        }

        /// <summary>
        /// Returns Tasks by patient reference.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="patientRef">Patient reference or id.</param>
        /// <param name="includeBasedOn">Whether to include based-on references.</param>
        /// <param name="includeRecipient">Whether to include recipient references.</param>
        /// <param name="listReturnLimit">Maximum number of results to return.</param>
        /// <param name="ct">Cancellation token.</param>
        public static Task<List<Task>> ByPatientAsync(ClientConfigurator configurator, string patientRef,
            bool includeBasedOn = false, bool includeRecipient = false, int listReturnLimit = SearchExecutor.DefaultServerMaxResults, CancellationToken ct = default)
        {
            var p = new TaskSearchParams
            {
                Patient = patientRef,
                IncludeBasedOn = includeBasedOn,
                IncludeRecipient = includeRecipient,
                ListReturnLimit = listReturnLimit
            };
            return SearchTasksAsync(configurator, p, ct);
        }
    }
}
