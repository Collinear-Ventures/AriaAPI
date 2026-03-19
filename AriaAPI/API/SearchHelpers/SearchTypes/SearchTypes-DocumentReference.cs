// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AriaAPI.API.SearchHelpers
{
    /// <summary>
    /// Partial class containing search type enumerations and mappings for FHIR resource searches.
    /// </summary>
    public static partial class SearchTypes
    {
        /// <summary>
        /// Enumerates the Aria-specific document types used for DocumentReference searches.
        /// </summary>
        public enum DocumentType
        {
            /// <summary>The Advance Directive document type.</summary>
            AdvanceDirective,
            /// <summary>The AURA document type.</summary>
            AURA,
            /// <summary>The Brachy Preplan document type.</summary>
            BrachyPreplan,
            /// <summary>The Brachy Treatment Plan document type.</summary>
            BrachyTreatmentPlan,
            /// <summary>The Brachy Treatment Record document type.</summary>
            BrachyTreatmentRecord,
            /// <summary>The Breast FU Questionnaire document type.</summary>
            BreastFUQuestionnaire,
            /// <summary>The Calculation document type.</summary>
            Calculation,
            /// <summary>The Calypso document type.</summary>
            Calypso,
            /// <summary>The Clinical Follow Up Note LANTIS document type.</summary>
            ClinicalFollowUpNoteLANTIS,
            /// <summary>The Clinical MD Note LANTIS document type.</summary>
            ClinicalMDNoteLANTIS,
            /// <summary>The Clinical Staff Note LANTIS document type.</summary>
            ClinicalStaffNoteLANTIS,
            /// <summary>The Consent document type.</summary>
            Consent,
            /// <summary>The Consent - Contrast document type.</summary>
            ConsentContrast,
            /// <summary>The Consent - General document type.</summary>
            ConsentGeneral,
            /// <summary>The Consent - LANTIS document type.</summary>
            ConsentLANTIS,
            /// <summary>The Consent - Research document type.</summary>
            ConsentResearch,
            /// <summary>The Consent - Treatment document type.</summary>
            ConsentTreatment,
            /// <summary>The Consult document type.</summary>
            Consult,
            /// <summary>The Consult Note - IHIS document type.</summary>
            ConsultNoteIHIS,
            /// <summary>The Consult Report - LANTIS document type.</summary>
            ConsultReportLANTIS,
            /// <summary>The Consult Request document type.</summary>
            ConsultRequest,
            /// <summary>The Course Summary document type.</summary>
            CourseSummary,
            /// <summary>The Downtime Forms document type.</summary>
            DowntimeForms,
            /// <summary>The Eclipse document type.</summary>
            Eclipse,
            /// <summary>The Follow-Up LANTIS document type.</summary>
            FollowUpLANTIS,
            /// <summary>The Gamma Knife Treatment Plan document type.</summary>
            GammaKnifeTreatmentPlan,
            /// <summary>The Identify Treatment Report document type.</summary>
            IdentifyTreatmentReport,
            /// <summary>The IGRT Shifts document type.</summary>
            IGRTShifts,
            /// <summary>The Image document type.</summary>
            Image,
            /// <summary>The Imaging Order document type.</summary>
            ImagingOrder,
            /// <summary>The IMRT Objectives document type.</summary>
            IMRTObjectives,
            /// <summary>The Lantis Clinical Notes document type.</summary>
            LantisClinicalNotes,
            /// <summary>The Lantis Tx Record document type.</summary>
            LantisTxRecord,
            /// <summary>The Miscellaneous document type.</summary>
            Miscellaneous,
            /// <summary>The Miscellaneous - LANTIS document type.</summary>
            MiscellaneousLANTIS,
            /// <summary>The Miscellaneous Correspondence document type.</summary>
            MiscellaneousCorrespondence,
            /// <summary>The Miscellaneous Notes - LANTIS document type.</summary>
            MiscellaneousNotesLANTIS,
            /// <summary>The Name/ID Note LANTIS (MRN change) document type.</summary>
            NameIDNoteLANTIS,
            /// <summary>The OTV Assessments document type.</summary>
            OTVAssessments,
            /// <summary>The Outside Medical Record document type.</summary>
            OutsideMedicalRecord,
            /// <summary>The Outside Treatment Plan document type.</summary>
            OutsideTreatmentPlan,
            /// <summary>The Pace/Defibrillator Documentation document type.</summary>
            PaceDefibrillatorDocumentation,
            /// <summary>The Pacemaker/ICD/Power Port Document document type.</summary>
            PacemakerICDPowerPortDocument,
            /// <summary>The Patient Education Note document type.</summary>
            PatientEducationNote,
            /// <summary>The Patient Exam Photos document type.</summary>
            PatientExamPhotos,
            /// <summary>The Patient Preferences document type.</summary>
            PatientPreferences,
            /// <summary>The Physics Note document type.</summary>
            PhysicsNote,
            /// <summary>The Planning Objectives document type.</summary>
            PlanningObjectives,
            /// <summary>The Procedure Note document type.</summary>
            ProcedureNote,
            /// <summary>The Progress Note document type.</summary>
            ProgressNote,
            /// <summary>The Quality Assurance document type.</summary>
            QualityAssurance,
            /// <summary>The Record Request document type.</summary>
            RecordRequest,
            /// <summary>The Release of Information document type.</summary>
            ReleaseOfInformation,
            /// <summary>The RT Peer Review Patient History document type.</summary>
            RTPeerReviewPatientHistory,
            /// <summary>The SimSetup document type.</summary>
            SimSetup,
            /// <summary>The Simulation Note document type.</summary>
            SimulationNote,
            /// <summary>The Simulation Order document type.</summary>
            SimulationOrder,
            /// <summary>The SSD Tracking Document document type.</summary>
            SSDTrackingDocument,
            /// <summary>The Test DB Tags document type.</summary>
            TestDBTags,
            /// <summary>The Treatment Plan document type.</summary>
            TreatmentPlan,
            /// <summary>The Treatment Planning Request document type.</summary>
            TreatmentPlanningRequest,
            /// <summary>The Treatment Prescriptions document type.</summary>
            TreatmentPrescriptions,
            /// <summary>The Verification Simulation document type.</summary>
            VerificationSimulation
        }

        /// <summary>
        /// Maps <see cref="DocumentType"/> enum values to their Aria display string representations.
        /// </summary>
        public static readonly IReadOnlyDictionary<DocumentType, string> DocumentTypeMap =
            new Dictionary<DocumentType, string>
            {
                { DocumentType.AdvanceDirective, "Advance Directive" },
                { DocumentType.AURA, "AURA" },
                { DocumentType.BrachyPreplan, "Brachy Preplan" },
                { DocumentType.BrachyTreatmentPlan, "Brachy Treatment Plan" },
                { DocumentType.BrachyTreatmentRecord, "Brachy Treatment Record" },
                { DocumentType.BreastFUQuestionnaire, "Breast FU Questionnaire" },
                { DocumentType.Calculation, "Calculation" },
                { DocumentType.Calypso, "Calypso" },
                { DocumentType.ClinicalFollowUpNoteLANTIS, "Clinical Follow Up Note LANTIS" },
                { DocumentType.ClinicalMDNoteLANTIS, "Clinical MD Note LANTIS" },
                { DocumentType.ClinicalStaffNoteLANTIS, "Clinical Staff Note LANTIS" },
                { DocumentType.Consent, "Consent" },
                { DocumentType.ConsentContrast, "Consent - Contrast" },
                { DocumentType.ConsentGeneral, "Consent - General" },
                { DocumentType.ConsentLANTIS, "Consent - LANTIS" },
                { DocumentType.ConsentResearch, "Consent - Research" },
                { DocumentType.ConsentTreatment, "Consent - Treatment" },
                { DocumentType.Consult, "Consult" },
                { DocumentType.ConsultNoteIHIS, "Consult Note - IHIS" },
                { DocumentType.ConsultReportLANTIS, "Consult Report - LANTIS" },
                { DocumentType.ConsultRequest, "Consult Request" },
                { DocumentType.CourseSummary, "Course Summary" },
                { DocumentType.DowntimeForms, "Downtime Forms" },
                { DocumentType.Eclipse, "Eclipse" },
                { DocumentType.FollowUpLANTIS, "Follow-Up - LANTIS" },
                { DocumentType.GammaKnifeTreatmentPlan, "Gamma Knife Treatment Plan" },
                { DocumentType.IdentifyTreatmentReport, "Identify Treatment Report" },
                { DocumentType.IGRTShifts, "IGRT shifts" },
                { DocumentType.Image, "Image" },
                { DocumentType.ImagingOrder, "Imaging Order" },
                { DocumentType.IMRTObjectives, "IMRT Objectives" },
                { DocumentType.LantisClinicalNotes, "Lantis Clinical Notes" },
                { DocumentType.LantisTxRecord, "Lantis Tx Record" },
                { DocumentType.Miscellaneous, "Miscellaneous" },
                { DocumentType.MiscellaneousLANTIS, "Miscellaneous - LANTIS" },
                { DocumentType.MiscellaneousCorrespondence, "Miscellaneous Correspondence" },
                { DocumentType.MiscellaneousNotesLANTIS, "Miscellaneous Notes - LANTIS" },
                { DocumentType.NameIDNoteLANTIS, "Name/I.D. Note LANTIS (MRN change)" },
                { DocumentType.OTVAssessments, "OTV Assessments" },
                { DocumentType.OutsideMedicalRecord, "Outside Medical Record" },
                { DocumentType.OutsideTreatmentPlan, "Outside Treatment Plan" },
                { DocumentType.PaceDefibrillatorDocumentation, "Pace/Defibrillator Documentation" },
                { DocumentType.PacemakerICDPowerPortDocument, "Pacemaker/ICD/Power Port Document" },
                { DocumentType.PatientEducationNote, "Patient Education Note" },
                { DocumentType.PatientExamPhotos, "Patient Exam Photos" },
                { DocumentType.PatientPreferences, "Patient Preferences" },
                { DocumentType.PhysicsNote, "Physics Note" },
                { DocumentType.PlanningObjectives, "Planning Objectives" },
                { DocumentType.ProcedureNote, "Procedure Note" },
                { DocumentType.ProgressNote, "Progress Note" },
                { DocumentType.QualityAssurance, "Quality Assurance" },
                { DocumentType.RecordRequest, "Record Request" },
                { DocumentType.ReleaseOfInformation, "Release of information" },
                { DocumentType.RTPeerReviewPatientHistory, "RT Peer Review Patient History" },
                { DocumentType.SimSetup, "SimSetup" },
                { DocumentType.SimulationNote, "Simulation Note" },
                { DocumentType.SimulationOrder, "Simulation Order" },
                { DocumentType.SSDTrackingDocument, "SSD Tracking Document" },
                { DocumentType.TestDBTags, "Test DB tags" },
                { DocumentType.TreatmentPlan, "Treatment Plan" },
                { DocumentType.TreatmentPlanningRequest, "Treatment Planning Request" },
                { DocumentType.TreatmentPrescriptions, "Treatment Prescriptions" },
                { DocumentType.VerificationSimulation, "Verification Simulation" }
            };
    }
}
