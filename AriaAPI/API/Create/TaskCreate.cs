// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿
using AriaAPI.Core;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeableConcept = Hl7.Fhir.Model.CodeableConcept;
using TaskResource = Hl7.Fhir.Model.Task;

namespace AriaAPI.API.TaskCreate
{
    /// <summary>
    /// Entry point to create FHIR Task resources for a patient.
    /// </summary>
    public static class TaskCreate
    {
        /// <summary>
        /// Parameters required to create a FHIR Task resource.
        /// </summary>
        public sealed class TaskCreateParams
        {
            /// <summary>FHIR reference to the patient, e.g. "Patient/123" (required for patient tasks).</summary>
            public string PatientReference { get; init; } = default!;

            /// <summary>Human readable description (Task.description). Optional but recommended.</summary>
            public string? Description { get; init; }

            /// <summary>Task code as CodeableConcept. Recommended for categorization/routing.</summary>
            public CodeableConcept? Code { get; init; }

            /// <summary>Task status: requested | accepted | rejected | in-progress | on-hold | completed | cancelled | entered-in-error.</summary>
            internal string Status { get; init; } = "ready";

            /// <summary>Task intent: unknown | proposal | plan | order | original-order | reflex-order | filler-order | instance-order | option.</summary>
            internal string Intent { get; init; } = "order";

            /// <summary>Optional focus of the task, e.g. "DocumentReference/xyz" or "ServiceRequest/abc".</summary>
            public string? FocusReference { get; init; }

            /// <summary>Optional authoredOn (if omitted, server may set). </summary>
            public DateTimeOffset? AuthoredOn { get; init; }

            /// <summary>Optional notes displayed in Task.note.</summary>
            public List<string>? Notes { get; init; }
            /// <summary>Duration extensions for the task. Defaults to a 10-minute duration using the Varian minutesDuration extension.</summary>
            public List<Extension> Duration { get; set; } = new List<Extension>() 
                {
                    new Extension()
                        {
                            Url = "http://varian.com/fhir/v1/StructureDefinition/task-minutesDuration",
                            Value = new FhirDecimal(10) // Default duration of 10 minutes
                        }
                };
            /// <summary>Optional restriction component defining who can fulfill the task and repetition limits.</summary>
            public TaskResource.RestrictionComponent Restriction { get; set; } = new TaskResource.RestrictionComponent();
        }

        /// <summary>
        /// Creates a Task on the FHIR server.
        /// </summary>
        public static async Task<TaskResource> CreateAsync(
            ClientConfigurator configurator,
            TaskCreateParams p,
            ILogger logger,
            CancellationToken ct = default)
        {
            if (configurator is null) throw new ArgumentNullException(nameof(configurator));
            if (p is null) throw new ArgumentNullException(nameof(p));

            if (string.IsNullOrWhiteSpace(p.PatientReference))
                throw new ArgumentException("PatientReference is required", nameof(p.PatientReference));

            ct.ThrowIfCancellationRequested();

            // Build Task
            var task = new TaskResource
            {
                Status = ParseStatus(p.Status),
                Intent = ParseIntent(p.Intent),

                // The patient the task is for
                For = new ResourceReference(p.PatientReference),

                Description = p.Description,

                Code = p.Code,

                AuthoredOnElement = p.AuthoredOn.HasValue
                    ? new FhirDateTime(p.AuthoredOn.Value.DateTime)
                    : null,
                Restriction = p.Restriction,
                Extension = p.Duration,

            };

            // Focus (e.g., DocumentReference)
            if (!string.IsNullOrWhiteSpace(p.FocusReference))
                task.Focus = new ResourceReference(p.FocusReference);

            // Notes
            if (p.Notes is { Count: > 0 })
            {
                task.Note = p.Notes
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Select(n => new Annotation { Text = n.Trim() })
                    .ToList();
            }

            // Create via resource client
            var client = configurator.ForResource<TaskResource>(ct);
            var created = await client.CreateAsync(task).ConfigureAwait(false);

            logger.LogInformation("Task created with id: {Id}.", created?.Id);

            return created!;
        }

        /// <summary>
        /// Valid FHIR Task status codes.
        /// </summary>
        private static readonly HashSet<string> ValidStatusCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "draft", "requested", "received", "accepted", "rejected", "ready",
            "cancelled", "in-progress", "on-hold", "failed", "completed", "entered-in-error"
        };

        /// <summary>
        /// Valid FHIR Task intent codes.
        /// </summary>
        private static readonly HashSet<string> ValidIntentCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "unknown", "proposal", "plan", "order", "original-order",
            "reflex-order", "filler-order", "instance-order", "option"
        };

        private static TaskResource.TaskStatus ParseStatus(string? s)
        {
            var normalized = (s ?? "requested").Trim().ToLowerInvariant();
            if (!ValidStatusCodes.Contains(normalized))
                throw new ArgumentException(
                    $"Invalid FHIR Task status '{s}'. Valid values: {string.Join(", ", ValidStatusCodes)}.", nameof(s));

            return normalized switch
            {
                "draft" => TaskResource.TaskStatus.Draft,
                "requested" => TaskResource.TaskStatus.Requested,
                "received" => TaskResource.TaskStatus.Received,
                "accepted" => TaskResource.TaskStatus.Accepted,
                "rejected" => TaskResource.TaskStatus.Rejected,
                "ready" => TaskResource.TaskStatus.Ready,
                "cancelled" => TaskResource.TaskStatus.Cancelled,
                "in-progress" => TaskResource.TaskStatus.InProgress,
                "on-hold" => TaskResource.TaskStatus.OnHold,
                "failed" => TaskResource.TaskStatus.Failed,
                "completed" => TaskResource.TaskStatus.Completed,
                "entered-in-error" => TaskResource.TaskStatus.EnteredInError,
                _ => throw new System.Diagnostics.UnreachableException($"Validated status '{normalized}' has no switch arm.")
            };
        }

        private static TaskResource.TaskIntent ParseIntent(string? s)
        {
            var normalized = (s ?? "order").Trim().ToLowerInvariant();
            if (!ValidIntentCodes.Contains(normalized))
                throw new ArgumentException(
                    $"Invalid FHIR Task intent '{s}'. Valid values: {string.Join(", ", ValidIntentCodes)}.", nameof(s));

            return normalized switch
            {
                "unknown" => TaskResource.TaskIntent.Unknown,
                "proposal" => TaskResource.TaskIntent.Proposal,
                "plan" => TaskResource.TaskIntent.Plan,
                "order" => TaskResource.TaskIntent.Order,
                "original-order" => TaskResource.TaskIntent.OriginalOrder,
                "reflex-order" => TaskResource.TaskIntent.ReflexOrder,
                "filler-order" => TaskResource.TaskIntent.FillerOrder,
                "instance-order" => TaskResource.TaskIntent.InstanceOrder,
                "option" => TaskResource.TaskIntent.Option,
                _ => throw new System.Diagnostics.UnreachableException($"Validated intent '{normalized}' has no switch arm.")
            };
        }


    }
}
