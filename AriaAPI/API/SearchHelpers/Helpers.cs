// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AriaAPI.API.SearchHelpers.SearchTypes;

namespace AriaAPI.API.SearchHelpers
{
    /// <summary>
    /// Provides utility methods for FHIR search operations including date window filtering,
    /// appointment category matching, and date parameter generation.
    /// </summary>
    public static class SearchHelpers
    {
        /// <summary>
        /// Returns the FHIR token code for ActivityDefinitionKind as lower-case (e.g., "appointment", "task").
        /// </summary>
        public static string ToCode(ActivityDefinitionKind kind) =>
            kind.ToString().ToLower(CultureInfo.InvariantCulture);

        /// <summary>
        /// Returns the FHIR token code for PublicationStatus as lower-case (e.g., "draft", "active").
        /// </summary>
        public static string ToCode(PublicationStatus status) =>
            status.ToString().ToLower(CultureInfo.InvariantCulture);
        /// <summary>
        /// Determines whether the appointment's start time falls within the specified date window (inclusive).
        /// </summary>
        /// <param name="a">The appointment to check.</param>
        /// <param name="start">The inclusive start of the date window.</param>
        /// <param name="end">The inclusive end of the date window.</param>
        /// <returns><see langword="true"/> if the appointment start is within the window; otherwise <see langword="false"/>.</returns>
        public static bool IsWithinDateWindow(Appointment a, DateTimeOffset start, DateTimeOffset end)
        {
            var s = GetStartInstant(a);
            if (!s.HasValue) return false; // appointments without a Start are excluded
            return s.Value >= start && s.Value <= end;
        }

        /// <summary>
        /// Extracts the appointment start as DateTimeOffset if available.
        /// Works with typical FHIR SDK shapes (Start or StartElement).
        /// </summary>
        public static DateTimeOffset? GetStartInstant(Appointment a)
        {
            // Firely SDK often exposes Appointment.Start as string (FHIR dateTime)
            // Fallback attempts to parse it as ISO-8601.
            try
            {
                // Prefer Start if available
                var startString = a?.Start.ToFhirDate(); // string? in many Firely models
                if (!string.IsNullOrWhiteSpace(startString) &&
                    DateTimeOffset.TryParse(startString, out var dto))
                {
                    return dto;
                }

                // If your model uses StartElement or a custom field, add additional parsing here:
                // return a?.StartElement?.ToDateTimeOffset(TimeSpan.Zero);

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Checks whether an appointment matches a given category display string.
        /// This tries multiple conventional FHIR fields for category-like labels.
        /// </summary>
        public static bool MatchesCategoryDisplay(Appointment a, string categoryDisplay)
        {
            if (string.IsNullOrWhiteSpace(categoryDisplay)) return false;

            // Check common fields for category semantics; compare text/display (case-insensitive).
            var candidates = GetCategoryDisplayCandidates(a);

            return candidates.Any(text =>
                text != null && text.Equals(categoryDisplay, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks whether an appointment matches any of the given category display strings.
        /// </summary>
        /// <param name="a">The appointment to check.</param>
        /// <param name="categoryDisplays">A set of category display strings to match against (case-sensitive via HashSet).</param>
        /// <returns><see langword="true"/> if any category candidate matches; otherwise <see langword="false"/>.</returns>
        public static bool MatchesAnyCategoryDisplay(Appointment a, HashSet<string> categoryDisplays)
        {
            if (categoryDisplays == null || categoryDisplays.Count == 0) return false;

            var candidates = GetCategoryDisplayCandidates(a);

            foreach (var text in candidates)
            {
                if (text != null && categoryDisplays.Contains(text))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Pulls possible “category display” texts from common Appointment fields.
        /// Adjust this if your ARIA/Varian mapping uses a specific field.
        /// </summary>
        public static IEnumerable<string?> GetCategoryDisplayCandidates(Appointment a)
        {
            // 1) serviceCategory (R4) — List<CodeableConcept>
            if (a?.ServiceCategory != null)
            {
                foreach (var cc in a.ServiceCategory)
                {
                    yield return cc?.Text;
                    if (cc?.Coding != null)
                    {
                        foreach (var coding in cc.Coding)
                            yield return coding?.Display;
                    }
                }
            }

            // 2) serviceType — List<CodeableConcept>
            if (a?.ServiceType != null)
            {
                foreach (var cc in a.ServiceType)
                {
                    yield return cc?.Text;
                    if (cc?.Coding != null)
                    {
                        foreach (var coding in cc.Coding)
                            yield return coding?.Display;
                    }
                }
            }

            // 3) appointmentType — CodeableConcept
            if (a?.AppointmentType != null)
            {
                yield return a.AppointmentType.Text;
                if (a.AppointmentType.Coding != null)
                {
                    foreach (var coding in a.AppointmentType.Coding)
                        yield return coding?.Display;
                }
            }

            // 4) reasonCode — List<CodeableConcept>
            if (a?.ReasonCode != null)
            {
                foreach (var cc in a.ReasonCode)
                {
                    yield return cc?.Text;
                    if (cc?.Coding != null)
                    {
                        foreach (var coding in cc.Coding)
                            yield return coding?.Display;
                    }
                }
            }
        }


        /// <summary>
        /// Converts optional start and end dates into FHIR date search parameter strings
        /// using inclusive day bounds (<c>ge</c> for start, <c>le</c> for end).
        /// </summary>
        /// <param name="start">The inclusive start date, or <see langword="null"/> to omit the lower bound.</param>
        /// <param name="end">The inclusive end date, or <see langword="null"/> to omit the upper bound.</param>
        /// <returns>An enumerable of FHIR date comparator strings (e.g., <c>ge2026-01-01</c>).</returns>
        public static IEnumerable<string> ToDateParams(DateTime? start, DateTime? end)
        {
            if (start.HasValue) yield return $"ge{start.Value:yyyy-MM-dd}";
            if (end.HasValue) yield return $"le{end.Value:yyyy-MM-dd}";
        }

    }
}
