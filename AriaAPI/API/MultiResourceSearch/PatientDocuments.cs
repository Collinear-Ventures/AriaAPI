// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿
using static AriaAPI.API.SearchHelpers.SearchHelpers;
using AriaAPI.API.SearchHelpers;
using AriaAPI.API.SingleResourceSearch;
using AriaAPI.Core;
using AriaAPI.Networking.Core;
using AriaAPI.Resources.Includes;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AriaAPI.API.SearchHelpers.SearchTypes;
using System;

namespace AriaAPI.API.MultiResourceSearch
{
    /// <summary>
    /// Provides multi-resource search helpers that retrieve a <see cref="Patient"/> and their related
    /// <see cref="DocumentReference"/> resources with optional document-type and date filtering.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This partial static class exposes overloads of <see cref="PatientWithDocumentsAsync(ClientConfigurator, PatientDocumentsSearchParams)"/>
    /// to orchestrate a two-step workflow:
    /// </para>
    /// <list type="number">
    ///   <item>Resolve a <see cref="Patient"/> by identifier/name (or use a known Patient logical id).</item>
    ///   <item>Retrieve <see cref="DocumentReference"/> resources for that patient, applying server-side filters.</item>
    /// </list>
    /// <para>
    /// Overloads are provided for historical usage patterns:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     An overload that accepts a <c>string</c> document type token (legacy style).
    ///   </item>
    ///   <item>
    ///     An overload that accepts <see cref="IEnumerable{T}"/> of <see cref="DocumentType"/> values and performs
    ///     OR semantics via a comma-joined list (FHIR token OR).
    ///   </item>
    /// </list>
    /// <para>
    /// If no matching patient is found, the methods return <c>(null, emptyList)</c>.
    /// </para>
    /// <para>
    /// All methods rely on <see cref="ClientConfigurator"/> to construct resource clients for FHIR searches and
    /// reuse authentication/connection settings.
    /// </para>
    /// </remarks>
    public static partial class MultiResourceSearch
    {
        /// <summary>
        /// Encapsulates the input parameters required to retrieve a <see cref="Patient"/> and that patient's
        /// <see cref="DocumentReference"/> items in a single orchestrated call.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Patient resolution supports either a free-text identifier/name query via
        /// <see cref="PatientIdentifierOrName"/> (mapped to <c>PatientSearchParams.NameOrIdentifier</c>)
        /// or a direct <see cref="PatientId"/> (FHIR logical id). When <see cref="PatientId"/> is supplied
        /// and <see cref="UsePatientLogicalIdWhenProvided"/> is <c>true</c> (default), the search skips the
        /// patient lookup step and uses the supplied logical id directly.
        /// </para>
        /// <para>
        /// Document filtering supports:
        /// </para>
        /// <list type="bullet">
        ///   <item>Type OR semantics via <see cref="Types"/> (comma-joined in one <c>type</c> parameter).</item>
        ///   <item>Date ranges via <see cref="StartDate"/> / <see cref="EndDate"/> (inclusive day semantics).</item>
        ///   <item>Status fields via <see cref="Status"/> and <see cref="DocStatus"/>.</item>
        ///   <item>Includes, sorting, and limit hints via <see cref="IncludeContent"/>, <see cref="SortByDateDescending"/>, and <see cref="ListReturnLimit"/>.</item>
        /// </list>
        /// </remarks>
        public sealed class PatientDocumentsSearchParams
        {
            /// <summary>
            /// Free-text query used to resolve a patient (e.g., MRN or name).
            /// Mapped to <c>PatientSearchParams.NameOrIdentifier</c>.
            /// Ignored when <see cref="PatientId"/> is supplied and <see cref="UsePatientLogicalIdWhenProvided"/> is <c>true</c>.
            /// </summary>
            public string? PatientIdentifierOrName { get; init; }

            /// <summary>
            /// Known patient logical id (FHIR <c>Patient.id</c>). When provided and
            /// <see cref="UsePatientLogicalIdWhenProvided"/> is <c>true</c>, the orchestrator bypasses
            /// free-text patient lookup and uses this id directly.
            /// </summary>
            public string? PatientId { get; init; }

            /// <summary>
            /// Document types to include (OR semantics). The values are mapped to FHIR token
            /// strings and emitted as a single comma-joined <c>type</c> parameter.
            /// </summary>
            public IEnumerable<DocumentType>? Types { get; init; }

            /// <summary>
            /// Resource status filter for <c>DocumentReference.status</c> (e.g., <c>current</c>).
            /// Defaults to <c>current</c>.
            /// </summary>
            public string? Status { get; init; } = "current";

            /// <summary>
            /// Document status detail filter for <c>DocumentReference.docStatus</c> (e.g., <c>final</c>).
            /// Optional; include only if your server supports this parameter.
            /// </summary>
            public string? DocStatus { get; init; }

            /// <summary>
            /// Start of the inclusive local date range to filter documents. When provided along with
            /// <see cref="EndDate"/>, the dates are translated into FHIR <c>date</c> comparators
            /// using <c>geYYYY-MM-DD</c> and <c>leYYYY-MM-DD</c>. Ignored if <see cref="AllDates"/> is <c>true</c>.
            /// </summary>
            public DateTime? StartDate { get; init; }

            /// <summary>
            /// End of the inclusive local date range to filter documents. See <see cref="StartDate"/>.
            /// Ignored if <see cref="AllDates"/> is <c>true</c>.
            /// </summary>
            public DateTime? EndDate { get; init; }

            /// <summary>
            /// When <c>true</c>, ignores <see cref="StartDate"/> and <see cref="EndDate"/> and does not apply date filtering.
            /// </summary>
            public bool AllDates { get; init; } = false;

            /// <summary>
            /// If <c>true</c>, requests that content (attachments) be included alongside the document references,
            /// typically via <c>_include=DocumentReference:content</c> (server permitting).
            /// </summary>
            public bool IncludeContent { get; init; } = true;

            /// <summary>
            /// When <c>true</c>, emits a server-side sort hint of <c>_sort=-date</c> (descending by date), if supported by the server.
            /// </summary>
            public bool SortByDateDescending { get; init; } = true;

            /// <summary>
            /// Final defensive cap on the number of documents to return. Also used as the server-side
            /// <c>_count</c> hint when building <see cref="SearchParams"/>.
            /// </summary>
            public int ListReturnLimit { get; init; } = SearchExecutor.DefaultServerMaxResults;

            /// <summary>
            /// When <c>true</c> and <see cref="PatientId"/> is specified, bypasses free-text patient lookup and uses the
            /// provided logical id. When <c>false</c>, the orchestrator will still attempt to fetch the <see cref="Patient"/>
            /// by id for validation (first match).
            /// </summary>
            public bool UsePatientLogicalIdWhenProvided { get; init; } = true;

            /// <summary>
            /// Optional arbitrary key/value pairs for additional, non-modeled parameters.
            /// This object is not consumed by default; extend the implementation where needed to apply these values.
            /// </summary>
            public IDictionary<string, string>? Extra { get; init; }
        }

        /// <summary>
        /// Orchestrates a two-step query that first resolves a <see cref="Patient"/> and then retrieves
        /// that patient's <see cref="DocumentReference"/> resources using the supplied filters.
        /// </summary>
        /// <param name="configurator">Client configurator providing resource clients and authentication.</param>
        /// <param name="p">Orchestration parameters for patient resolution and document filtering.</param>
        /// <returns>
        /// A tuple <c>(patient, documents)</c> where <c>patient</c> is the resolved <see cref="Patient"/> (or <c>null</c> if not found)
        /// and <c>documents</c> is a (possibly empty) list of <see cref="DocumentReference"/> matching the filters.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Patient resolution:
        /// </para>
        /// <list type="bullet">
        ///   <item>
        ///     If <see cref="PatientDocumentsSearchParams.PatientId"/> is provided and
        ///     <see cref="PatientDocumentsSearchParams.UsePatientLogicalIdWhenProvided"/> is <c>true</c>,
        ///     the patient lookup is skipped and the id is used directly.
        ///   </item>
        ///   <item>
        ///     Otherwise, the orchestrator uses <see cref="PatientDocumentsSearchParams.PatientIdentifierOrName"/> to perform
        ///     a free-text lookup via <c>PatientSearch.PatientAsync</c> (first match).
        ///   </item>
        /// </list>
        /// <para>
        /// Document retrieval:
        /// </para>
        /// <list type="bullet">
        ///   <item>Types are joined with OR semantics and sent as a single <c>type</c> token parameter.</item>
        ///   <item>Date range is translated into FHIR <c>date</c> comparators using <see cref="ToDateParams(DateTime?, DateTime?)"/>.</item>
        ///   <item>Includes, reverse includes, sort hints, and count hints are passed through to the <c>DocumentReferenceSearch</c> layer.</item>
        /// </list>
        /// </remarks>
        /// <example>
        /// Resolve patient by MRN-like free text and fetch "current" documents between two dates:
        /// <code><![CDATA[
        /// var (pat, docs) = await MultiResourceSearch.PatientWithDocumentsAsync(cfg, new PatientDocumentsSearchParams {
        ///     PatientIdentifierOrName = "1234567",
        ///     Types = new[] { DocumentType.TreatmentPlan, DocumentType.SimulationNote },
        ///     StartDate = new DateTime(2026, 01, 01),
        ///     EndDate   = new DateTime(2026, 01, 21),
        ///     IncludeContent = true,
        ///     SortByDateDescending = true,
        ///     ListReturnLimit = 500
        /// });
        /// ]]></code>
        /// </example>
        public static async Task<(Patient? patient, List<DocumentReference> documents)>
            PatientWithDocumentsAsync(
                ClientConfigurator configurator,
                PatientDocumentsSearchParams p)
        {
            if (configurator is null) throw new ArgumentNullException(nameof(configurator));
            p ??= new PatientDocumentsSearchParams();

            // 1) Resolve patient (by logical id or by free-text)
            Patient? patient = null;
            string? patientId = p.PatientId;

            if (string.IsNullOrWhiteSpace(patientId))
            {
                if (string.IsNullOrWhiteSpace(p.PatientIdentifierOrName))
                    return (null, new List<DocumentReference>());

                // Use existing PatientSearch plumbing to find the first matching patient.
                patient = await PatientSearch.PatientAsync(
                    configurator,
                    new PatientSearch.PatientSearchParams
                    {
                        NameOrIdentifier = p.PatientIdentifierOrName,
                        Active = true
                    },
                    listReturnLimit: 1).ConfigureAwait(false);

                if (patient is null) return (null, new List<DocumentReference>());

                patientId = patient.Id ?? throw new InvalidOperationException("Patient Id cannot be null.");
            }
            else
            {
                // If caller supplied PatientId and wants to trust it, skip lookup.
                // Otherwise, validate by fetching the first matching Patient.
                if (!p.UsePatientLogicalIdWhenProvided)
                {
                    patient = await PatientSearch.PatientAsync(configurator, patientId, listReturnLimit: 1).ConfigureAwait(false);
                }
            }

            // 2) Validate date range before building search params.
            ValidateDateRange(p.StartDate, p.EndDate, p.AllDates);

            // 3) Build DocumentReference search params using server-side filters where possible.
            var docParams = new DocumentReferenceSearch.DocumentReferenceSearchParams
            {
                Patient = patientId,
                Status = string.IsNullOrWhiteSpace(p.Status) ? "current" : p.Status,
                DocStatus = p.DocStatus,
                Types = p.Types,
                // Convert Start/End into FHIR date comparators unless AllDates is true.
                Dates = p.AllDates ? null : ToDateParams(p.StartDate, p.EndDate),
                IncludeContent = p.IncludeContent,
                SortByDateDescending = p.SortByDateDescending,
                Count = p.ListReturnLimit
            };

            // NOTE: For advanced cases, you can extend DocumentReferenceSearchParams to accept arbitrary
            // query keys; 'Extra' is captured here for future use but not applied.

            // 4) Execute document search and return the result tuple.
            var documents = await DocumentReferenceSearch.DocumentsAsync(configurator, docParams, p.ListReturnLimit).ConfigureAwait(false);
            return (patient, documents);
        }

        /// <summary>
        /// Backward-compatible overload that accepts a free-text patient identifier and a set of document types,
        /// delegating to the main orchestrator. Uses OR semantics for the supplied types.
        /// </summary>
        /// <param name="configurator">Client configurator providing resource clients and authentication.</param>
        /// <param name="patientIdentifier">Free-text MRN/name to resolve the patient (first match).</param>
        /// <param name="documentTypes">Zero or more <see cref="DocumentType"/> filters (OR semantics).</param>
        /// <param name="listReturnLimit">Final defensive cap on returned <see cref="DocumentReference"/> items.</param>
        /// <returns>Tuple of (<see cref="Patient"/>, <see cref="List{T}"/> of <see cref="DocumentReference"/>).</returns>
        /// <remarks>
        /// If <paramref name="documentTypes"/> is <c>null</c> or empty, all document types are considered (subject to server defaults).
        /// </remarks>
        public static async Task<(Patient? patient, List<DocumentReference> documents)>
            PatientWithDocumentsAsync(
                ClientConfigurator configurator,
                string patientIdentifier,
                IEnumerable<DocumentType>? documentTypes,
                int listReturnLimit = -1)
        {
            // Backward-compat: callers that pass -1 (the documented default) mean "no cap".
            // int.MaxValue flows into ListReturnLimit intentionally so existing callers are unaffected.
            if (listReturnLimit <= 0) listReturnLimit = int.MaxValue;

            var (pat, docs) = await PatientWithDocumentsAsync(
                configurator,
                new PatientDocumentsSearchParams
                {
                    PatientIdentifierOrName = patientIdentifier,
                    Types = documentTypes,
                    ListReturnLimit = listReturnLimit
                }).ConfigureAwait(false);

            return (pat, docs);
        }

        /// <summary>
        /// Backward-compatible overload that accepts a free-text patient identifier and a single
        /// document type token (legacy string form). Delegates to the main orchestrator.
        /// </summary>
        /// <param name="configurator">Client configurator providing resource clients and authentication.</param>
        /// <param name="patientIdentifier">Free-text MRN/name to resolve the patient (first match).</param>
        /// <param name="documentType">
        /// Single document type token (legacy). If you maintain a mapping from token to <see cref="DocumentType"/>,
        /// you can translate here and pass strongly-typed values into the main orchestrator.
        /// </param>
        /// <param name="listReturnLimit">Final defensive cap on returned <see cref="DocumentReference"/> items.</param>
        /// <returns>Tuple of (<see cref="Patient"/>, <see cref="List{T}"/> of <see cref="DocumentReference"/>).</returns>
        /// <remarks>
        /// For modern usage, prefer the overload accepting <see cref="IEnumerable{T}"/> of <see cref="DocumentType"/>.
        /// </remarks>
        public static async Task<(Patient? patient, List<DocumentReference> documents)>
            PatientWithDocumentsAsync(
                ClientConfigurator configurator,
                string patientIdentifier,
                string documentType = "",
                int listReturnLimit = -1)
        {
            // Backward-compat: callers that pass -1 (the documented default) mean "no cap".
            // int.MaxValue flows into ListReturnLimit intentionally so existing callers are unaffected.
            if (listReturnLimit <= 0) listReturnLimit = int.MaxValue;

            // Optionally translate the legacy token string into a DocumentType here if you have a mapping.
            IEnumerable<DocumentType>? types = null;
            if (!string.IsNullOrWhiteSpace(documentType))
            {
                // If available, parse/translate 'documentType' into one or more DocumentType values and assign to 'types'.
                // Leaving null here defers filtering to server defaults or other layers.
            }

            var (pat, docs) = await PatientWithDocumentsAsync(
                configurator,
                new PatientDocumentsSearchParams
                {
                    PatientIdentifierOrName = patientIdentifier,
                    Types = types,
                    ListReturnLimit = listReturnLimit
                }).ConfigureAwait(false);

            return (pat, docs);
        }

        /// <summary>
        /// Validates that the supplied date range is coherent and within allowed bounds.
        /// </summary>
        /// <param name="start">Inclusive start date, or <c>null</c> for no lower bound.</param>
        /// <param name="end">Inclusive end date, or <c>null</c> for no upper bound.</param>
        /// <param name="allDates">
        /// When <c>true</c> the caller has explicitly opted into an unbounded query and all
        /// validation is skipped.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="end"/> is in the future, or when the range exceeds 730 days.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="start"/> is later than <paramref name="end"/>.
        /// </exception>
        internal static void ValidateDateRange(DateTime? start, DateTime? end, bool allDates)
        {
            if (allDates) return; // caller explicitly opted into unbounded — no cap applied

            if (end.HasValue && end.Value.Date > DateTime.Today)
                throw new ArgumentOutOfRangeException(nameof(end), "EndDate must not be in the future.");

            if (start.HasValue && end.HasValue && start.Value > end.Value)
                throw new ArgumentException("StartDate must not be later than EndDate.");

            if (start.HasValue && end.HasValue && (end.Value - start.Value).TotalDays > 730)
                throw new ArgumentOutOfRangeException(nameof(end), "Date range must not exceed 2 years (730 days). Use AllDates=true for unbounded queries.");
        }
    }
}
