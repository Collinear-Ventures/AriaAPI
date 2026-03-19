// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AriaAPI.API.SearchHelpers
{
    /// <summary>
    /// Provides strongly-typed enumerations and mapping dictionaries for FHIR search parameter values
    /// across multiple resource types (Appointment, Device, Location, Observation, etc.).
    /// </summary>
    public static partial class SearchTypes
    {
        /// <summary>
        /// Categories used for appointment and activity classification.
        /// </summary>
        public enum AppointmentCategory
        {
            /// <summary>The Blocking category.</summary>
            Blocking,
            /// <summary>The Brachytherapy category.</summary>
            Brachytherapy,
            /// <summary>The Chart Rounds category.</summary>
            ChartRounds,
            /// <summary>The C-Port Film category.</summary>
            CPortFilm,
            /// <summary>The Department Meeting category.</summary>
            DepartmentMeeting,
            /// <summary>The Diagnostic Studies category.</summary>
            DiagnosticStudies,
            /// <summary>The Dosimetry category.</summary>
            Dosimetry,
            /// <summary>The Exam category.</summary>
            Exam,
            /// <summary>The Historical Activity Category.</summary>
            HistoricalActivityCategory,
            /// <summary>The Hospital Care category.</summary>
            HospitalCare,
            /// <summary>The Hyperthermia category.</summary>
            Hyperthermia,
            /// <summary>The Insurance category.</summary>
            Insurance,
            /// <summary>The Maintenance category.</summary>
            Maintenance,
            /// <summary>The Medical Records category.</summary>
            MedicalRecords,
            /// <summary>The Nursing category.</summary>
            Nursing,
            /// <summary>The Office Tasks category.</summary>
            OfficeTasks,
            /// <summary>The Oncologist Tasks category.</summary>
            OncologistTasks,
            /// <summary>The Other Procedures category.</summary>
            OtherProcedures,
            /// <summary>The Patient On Break category.</summary>
            PatientOnBreak,
            /// <summary>The Personal category.</summary>
            Personal,
            /// <summary>The Physician Cognitive category.</summary>
            PhysicianCognitive,
            /// <summary>The Physician Orders category.</summary>
            PhysicianOrders,
            /// <summary>The Physics category.</summary>
            Physics,
            /// <summary>The Planning Tasks category.</summary>
            PlanningTasks,
            /// <summary>The Pre-Simulation category.</summary>
            PreSimulation,
            /// <summary>The Professional Meeting category.</summary>
            ProfessionalMeeting,
            /// <summary>The Registration category.</summary>
            Registration,
            /// <summary>The Simulation category.</summary>
            Simulation,
            /// <summary>The Simulation Films category.</summary>
            SimulationFilms,
            /// <summary>The Simulation Tasks category.</summary>
            SimulationTasks,
            /// <summary>The Special Review Activities category.</summary>
            SpecialReviewActivities,
            /// <summary>The Staff Meeting category.</summary>
            StaffMeeting,
            /// <summary>The Transcription category.</summary>
            Transcription,
            /// <summary>The Treatment category.</summary>
            Treatment,
            /// <summary>The Treatment Devices category.</summary>
            TreatmentDevices,
            /// <summary>The Treatment Management category.</summary>
            TreatmentManagement,
            /// <summary>The Treatment Tasks category.</summary>
            TreatmentTasks
        }

        /// <summary>
        /// Maps <see cref="AppointmentCategory"/> values to the Aria display/token values
        /// used by the FHIR <c>category</c> search parameter.
        /// </summary>
        public static readonly IReadOnlyDictionary<AppointmentCategory, string> AppointmentCategoryMap =
            new Dictionary<AppointmentCategory, string>
            {
                { AppointmentCategory.Blocking, "Blocking" },
                { AppointmentCategory.Brachytherapy, "Brachytherapy" },
                { AppointmentCategory.ChartRounds, "Chart Rounds" },
                { AppointmentCategory.CPortFilm, "C-Port Film" },
                { AppointmentCategory.DepartmentMeeting, "Department Meeting" },
                { AppointmentCategory.DiagnosticStudies, "Diagnostic Studies" },
                { AppointmentCategory.Dosimetry, "Dosimetry" },
                { AppointmentCategory.Exam, "Exam" },
                { AppointmentCategory.HistoricalActivityCategory, "Historical Activity Category" },
                { AppointmentCategory.HospitalCare, "Hospital Care" },
                { AppointmentCategory.Hyperthermia, "Hyperthermia" },
                { AppointmentCategory.Insurance, "Insurance" },
                { AppointmentCategory.Maintenance, "Maintenance" },
                { AppointmentCategory.MedicalRecords, "Medical Records" },
                { AppointmentCategory.Nursing, "Nursing" },
                { AppointmentCategory.OfficeTasks, "Office Tasks" },
                { AppointmentCategory.OncologistTasks, "Oncologist Tasks" },
                { AppointmentCategory.OtherProcedures, "Other Procedures" },
                { AppointmentCategory.PatientOnBreak, "Patient On Break" },
                { AppointmentCategory.Personal, "Personal" },
                { AppointmentCategory.PhysicianCognitive, "Physician Cognitive" },
                { AppointmentCategory.PhysicianOrders, "Physician Orders" },
                { AppointmentCategory.Physics, "Physics" },
                { AppointmentCategory.PlanningTasks, "Planning Tasks" },
                { AppointmentCategory.PreSimulation, "Pre-Simulation" },
                { AppointmentCategory.ProfessionalMeeting, "Professional Meeting" },
                { AppointmentCategory.Registration, "Registration" },
                { AppointmentCategory.Simulation, "Simulation" },
                { AppointmentCategory.SimulationFilms, "Simulation Films" },
                { AppointmentCategory.SimulationTasks, "Simulation Tasks" },
                { AppointmentCategory.SpecialReviewActivities, "Special Review Activities" },
                { AppointmentCategory.StaffMeeting, "Staff Meeting" },
                { AppointmentCategory.Transcription, "Transcription" },
                { AppointmentCategory.Treatment, "Treatment" },
                { AppointmentCategory.TreatmentDevices, "Treatment Devices" },
                { AppointmentCategory.TreatmentManagement, "Treatment Management" },
                { AppointmentCategory.TreatmentTasks, "Treatment Tasks" }
            };
    }
}
