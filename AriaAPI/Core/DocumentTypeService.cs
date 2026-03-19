// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿
using AriaAPI.API.SingleResourceSearch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static AriaAPI.API.SearchHelpers.SearchTypes;


namespace AriaAPI.Core
{
    /// <summary>
    /// Dynamically expands the Varian DocumentReference/documentreference-type ValueSet,
    /// builds lookups, and resolves CodeableConcepts for your DocumentType enum.
    /// </summary>
    public sealed class DocumentTypeConceptService
    {
        private readonly object _sync = new();
        private bool _initialized;

        // Lookups (case-insensitive)
        private Dictionary<string, CodeRow> _byDisplay = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, CodeRow> _byCode = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>All code rows, useful for binding/select lists.</summary>
        public List<CodeRow> Options { get; private set; } = [];

        /// <summary>
        /// When non-null, rows are filtered to this system during initialization.
        /// Recommended: "http://varian.com/fhir/CodeSystem/DocumentReference/documentreference-type"
        /// </summary>
        public string? PreferredSystem { get; }

        /// <summary>
        /// Optional aliases mapping enum values to the ValueSet Display strings.
        /// Extend to match local naming; only exact Display matches resolve.
        /// </summary>
        private static readonly Dictionary<DocumentType, string> _displayAliases =
            new(EqualityComparer<DocumentType>.Default)
            {
            // Direct matches (from your list and the known ValueSet)
            { DocumentType.AdvanceDirective,               "Advance Directive" },
            { DocumentType.AURA,                           "AURA" },
            { DocumentType.Calculation,                    "Calculation" },
            { DocumentType.Calypso,                        "Calypso" },
            { DocumentType.ClinicalFollowUpNoteLANTIS,     "Follow-Up - LANTIS" },
            { DocumentType.ConsultNoteIHIS,                "Consult Note - IHIS" },
            { DocumentType.ConsultReportLANTIS,            "Consult Report - LANTIS" },
            { DocumentType.ConsultRequest,                 "Consult Request" },
            { DocumentType.CourseSummary,                  "Course Summary" },
            { DocumentType.DowntimeForms,                  "Downtime Forms" },
            { DocumentType.Eclipse,                        "Eclipse" },
            { DocumentType.FollowUpLANTIS,                 "Follow-Up - LANTIS" },
            { DocumentType.IGRTShifts,                     "IGRT shifts" },
            { DocumentType.Image,                          "Image" },
            { DocumentType.ImagingOrder,                   "Imaging Order" },
            { DocumentType.LantisClinicalNotes,            "Lantis Clinical Notes" },
            { DocumentType.LantisTxRecord,                 "Lantis Tx Record" },
            { DocumentType.Miscellaneous,                  "Miscellaneous" },
            { DocumentType.MiscellaneousCorrespondence,    "Miscellaneous Correspondence" },
            { DocumentType.MiscellaneousLANTIS,            "Miscellaneous - LANTIS" },
            { DocumentType.OTVAssessments,                 "OTV Assessments" },
            { DocumentType.OutsideMedicalRecord,           "Outside Medical Record" },
            { DocumentType.OutsideTreatmentPlan,           "Outside Treatment Plan" },
            { DocumentType.PacemakerICDPowerPortDocument,  "Pacemaker/ICD/Power Port Document" },
            { DocumentType.PatientEducationNote,           "Patient Education Note" },
            { DocumentType.PatientExamPhotos,              "Patient Exam Photos" },
            { DocumentType.PatientPreferences,             "Patient Preferences" },
            { DocumentType.PhysicsNote,                    "Physics Note" },
            { DocumentType.PlanningObjectives,             "Planning Objectives" },
            { DocumentType.ProcedureNote,                  "Procedure Note" },
            { DocumentType.QualityAssurance,               "Quality Assurance" },
            { DocumentType.RecordRequest,                  "Record Request" },
            { DocumentType.ReleaseOfInformation,           "Release of information" },
            { DocumentType.RTPeerReviewPatientHistory,     "RT Peer Review Patient History" },
            { DocumentType.SimSetup,                       "SimSetup" },
            { DocumentType.SimulationNote,                 "Simulation Note" },
            { DocumentType.SimulationOrder,                "Simulation Order" },
            { DocumentType.SSDTrackingDocument,            "SSD Tracking Document" },
            { DocumentType.TestDBTags,                     "Test DB tags" },
            { DocumentType.TreatmentPlan,                  "Treatment Plan" },
            { DocumentType.TreatmentPlanningRequest,       "Treatment Planning Request" },
            { DocumentType.TreatmentPrescriptions,         "Treatment Prescriptions" },
            { DocumentType.VerificationSimulation,         "Verification Simulation" },

            // Consents (align to known ValueSet entries)
            { DocumentType.ConsentContrast,                "Consent - Contrast" },
            { DocumentType.ConsentGeneral,                 "Consent - General" },
            { DocumentType.ConsentLANTIS,                  "Consent - LANTIS" },
            { DocumentType.ConsentResearch,                "Consent - Research" },
            { DocumentType.ConsentTreatment,               "Consent - Treatment" },

                // If you want "Consent" to default to General, uncomment:
                // { DocumentType.Consent,                     "Consent - General" },

                // The following enum values are present in your list but do not
                // exist in the known ValueSet snapshot provided earlier. They will
                // resolve as Unknown unless your ValueSet contains them:
                // BrachyPreplan, BrachyTreatmentPlan, BrachyTreatmentRecord,
                // BreastFUQuestionnaire, ClinicalMDNoteLANTIS, ClinicalStaffNoteLANTIS,
                // GammaKnifeTreatmentPlan, IdentifyTreatmentReport, IMRTObjectives,
                // MiscellaneousNotesLANTIS, NameIDNoteLANTIS, PaceDefibrillatorDocumentation,
                // ProgressNote, Consult (ambiguous against Consult Request)
            };

        private DocumentTypeConceptService(string? preferredSystem)
        {
            PreferredSystem = preferredSystem;
        }

        /// <summary>
        /// Factory that expands the ValueSet and returns a ready-to-use service.
        /// </summary>
        public static async Task<DocumentTypeConceptService> CreateAsync(
            ClientConfigurator configurator,
            string publisher = "Organization-Prov-7",
            int listReturnLimit = 250,
            string? preferredSystem = "http://varian.com/fhir/CodeSystem/DocumentReference/documentreference-type")
        {
            var svc = new DocumentTypeConceptService(preferredSystem);
            await svc.InitializeAsync(configurator, publisher, listReturnLimit).ConfigureAwait(false);
            return svc;
        }

        /// <summary>
        /// Resolve from enum. Returns Unknown when the enum cannot be matched to a ValueSet display.
        /// </summary>
        public CodeableConcept Resolve(DocumentType type)
        {
            EnsureInitialized();

            var display = _displayAliases.TryGetValue(type, out var d) ? d : null;
            if (!string.IsNullOrWhiteSpace(display) && _byDisplay.TryGetValue(display, out var row))
                return new CodeableConcept(row.System, row.Code, row.Display, row.Display);

            return Unknown();
        }

        /// <summary>Resolve by code (string). Returns null when not found.</summary>
        public CodeableConcept? TryResolveByCode(string code)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(code)) return null;
            return _byCode.TryGetValue(code, out var row)
                ? new CodeableConcept(row.System, row.Code, row.Display, row.Display)
                : null;
        }

        /// <summary>Resolve by display text. Returns null when not found.</summary>
        public CodeableConcept? TryResolveByDisplay(string display)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(display)) return null;
            return _byDisplay.TryGetValue(display, out var row)
                ? new CodeableConcept(row.System, row.Code, row.Display, row.Display)
                : null;
        }

        // ===== internals =====

        private async Task InitializeAsync(ClientConfigurator configurator, string publisher, int listReturnLimit)
        {
            // 1) Expand the ValueSet dynamically
            var expandedVs = await ValueSetSearch.ValueSetExpand.ExpandAsync(
                configurator,
                AriaValueSet.DocumentReferenceType,
                publisher: publisher,
                listReturnLimit: listReturnLimit
            ).ConfigureAwait(false);

            // 2) Project to CodeRow list
            var rows = expandedVs?.Expansion?.Contains?
                .Select(c => new CodeRow(c.System!, c.Code!, c.Display!))
                .ToList() ?? [];

            // 3) Optionally filter to a preferred system
            if (!string.IsNullOrWhiteSpace(PreferredSystem))
                rows = [.. rows.Where(r => string.Equals(r.System, PreferredSystem, StringComparison.OrdinalIgnoreCase))];

            // 4) Build lookups (first match wins)
            var byDisplay = new Dictionary<string, CodeRow>(StringComparer.OrdinalIgnoreCase);
            var byCode = new Dictionary<string, CodeRow>(StringComparer.OrdinalIgnoreCase);

            foreach (var r in rows)
            {
                if (!byDisplay.ContainsKey(r.Display)) byDisplay[r.Display] = r;
                if (!byCode.ContainsKey(r.Code)) byCode[r.Code] = r;
            }

            lock (_sync)
            {
                _byDisplay = byDisplay;
                _byCode = byCode;
                Options = rows;
                _initialized = true;
            }
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            throw new InvalidOperationException("DocumentTypeConceptService has not been initialized. Use CreateAsync(...) first.");
        }

        private static CodeableConcept Unknown() =>
            new("urn:aria:document-type", "unknown", "Unknown Document Type");
    }
}