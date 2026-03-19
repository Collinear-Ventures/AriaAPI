// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using Hl7.Fhir.Model;
using Hl7.Fhir.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static AriaAPI.API.SearchHelpers.SearchTypes;

namespace AriaAPI.API.Create
{
    /// <summary>
    /// Provides utility methods for creating FHIR resources, including appointment date
    /// filtering, category matching, content type mapping, hashing, and document type conversion.
    /// </summary>
    public static class CreateHelpers
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
        /// Maps file extensions to MIME content types for FHIR Attachment resources.
        /// </summary>
        internal static class ContentTypeHelper
        {
            /// <summary>
            /// Returns the MIME content type for the given file extension.
            /// Defaults to <c>application/octet-stream</c> for unrecognized extensions.
            /// </summary>
            /// <param name="ext">The file extension including the leading dot (e.g., ".pdf").</param>
            /// <returns>The corresponding MIME content type string.</returns>
            public static string MapFromExtension(string? ext)
            {
                ext = (ext ?? string.Empty).Trim().ToLowerInvariant();
                return ext switch
                {
                    ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    ".doc" => "application/msword",
                    ".pdf" => "application/pdf",
                    _ => "application/octet-stream"
                };
            }
        }

        /// <summary>
        /// Provides SHA-256 hashing utilities for document content integrity verification.
        /// </summary>
        internal static class HashHelper
        {
            /// <summary>
            /// Computes the SHA-256 hash of the given byte array and returns it as a lowercase hexadecimal string.
            /// </summary>
            /// <param name="bytes">The byte array to hash.</param>
            /// <returns>A lowercase hexadecimal SHA-256 hash string.</returns>
            public static string Sha256Hex(byte[] bytes)
            {
                using var sha = SHA256.Create();
                var hash = sha.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// Maps your domain's DocumentType enum to a FHIR CodeableConcept.
        /// Keep the coding system consistent with your server's expectations.
        /// </summary>
        internal static class DocumentTypeMapper
        {
            /// <summary>
            /// Converts a <see cref="DocumentType"/> enum value to the corresponding FHIR <see cref="CodeableConcept"/>.
            /// </summary>
            /// <param name="type">The document type to map.</param>
            /// <returns>A <see cref="CodeableConcept"/> with the appropriate system, code, and display values.</returns>
            public static CodeableConcept ToCodeableConcept(DocumentType type)
            {
                // Example mapping. Replace with your authoritative system/code.
                // If you already have DocumentType.ToCodeableConcept(), use that instead.
                return type switch
                {
                    DocumentType.ProcedureNote => new CodeableConcept(
                        system: "http://varian.com/fhir/CodeSystem/DocumentReference/documentreference-type",
                        code: "stp",
                        display: "Special Treatment Procedure Order",
                        text: "Special Treatment Procedure Order"),

                    DocumentType.TreatmentPrescriptions => new CodeableConcept(
                       system: "http://varian.com/fhir/CodeSystem/DocumentReference/documentreference-type",
                       code: "rx",
                       display: "Holiday Treatment Order",
                       text: "Holiday Treatment Order"),

                    _ => new CodeableConcept("urn:aria:document-type", "unknown", "Unknown Document Type")
                };
            }
        }

      


    }
}
