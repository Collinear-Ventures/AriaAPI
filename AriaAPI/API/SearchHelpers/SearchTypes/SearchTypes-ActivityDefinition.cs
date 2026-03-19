// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using Hl7.Fhir.Model;
using System;
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
        /// Local, SDK-agnostic enumerations for ActivityDefinition.kind (restricted to Appointment | Task).
        /// </summary>
        public enum ActivityDefinitionKind
        {
            /// <summary>The Appointment kind.</summary>
            Appointment,
            /// <summary>The Task kind.</summary>
            Task
        }

        /// <summary>
        /// Strongly-typed category codes for ActivityDefinition, as defined in Aria.
        /// </summary>
        public enum ActivityCategoryCode
        {
            /// <summary>The Brachytherapy category.</summary>
            Brachytherapy,
            /// <summary>The Department Meeting category.</summary>
            DepartmentMeeting,
            /// <summary>The Dosimetry category.</summary>
            Dosimetry,
            /// <summary>The Exam category.</summary>
            Exam,
            /// <summary>The Historical Activity Category.</summary>
            HistoricalActivityCategory,
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
            /// <summary>The Physician Orders category.</summary>
            PhysicianOrders,
            /// <summary>The Physics category.</summary>
            Physics,
            /// <summary>The Planning Tasks category.</summary>
            PlanningTasks,
            /// <summary>The Professional Meeting category.</summary>
            ProfessionalMeeting,
            /// <summary>The Simulation category.</summary>
            Simulation,
            /// <summary>The Special Review Activities category.</summary>
            SpecialReviewActivities,
            /// <summary>The Staff Meeting category.</summary>
            StaffMeeting,
            /// <summary>The Treatment category.</summary>
            Treatment,
            /// <summary>The Treatment Tasks category.</summary>
            TreatmentTasks
        }

        /// <summary>
        /// Maps typed <see cref="ActivityCategoryCode"/> values to the Aria display/token values
        /// used by the FHIR <c>category</c> search parameter.
        /// </summary>
        public static readonly IReadOnlyDictionary<ActivityCategoryCode, string> ActivityCategoryMap =
            new Dictionary<ActivityCategoryCode, string>
            {
                [ActivityCategoryCode.Brachytherapy] = "Brachytherapy",
                [ActivityCategoryCode.DepartmentMeeting] = "Department Meeting",
                [ActivityCategoryCode.Dosimetry] = "Dosimetry",
                [ActivityCategoryCode.Exam] = "Exam",
                [ActivityCategoryCode.HistoricalActivityCategory] = "Historical Activity Category",
                [ActivityCategoryCode.Maintenance] = "Maintenance",
                [ActivityCategoryCode.MedicalRecords] = "Medical Records",
                [ActivityCategoryCode.Nursing] = "Nursing",
                [ActivityCategoryCode.OfficeTasks] = "Office Tasks",
                [ActivityCategoryCode.OncologistTasks] = "Oncologist Tasks",
                [ActivityCategoryCode.OtherProcedures] = "Other Procedures",
                [ActivityCategoryCode.PatientOnBreak] = "Patient On Break",
                [ActivityCategoryCode.Personal] = "Personal",
                [ActivityCategoryCode.PhysicianOrders] = "Physician Orders",
                [ActivityCategoryCode.Physics] = "Physics",
                [ActivityCategoryCode.PlanningTasks] = "Planning Tasks",
                [ActivityCategoryCode.ProfessionalMeeting] = "Professional Meeting",
                [ActivityCategoryCode.Simulation] = "Simulation",
                [ActivityCategoryCode.SpecialReviewActivities] = "Special Review Activities",
                [ActivityCategoryCode.StaffMeeting] = "Staff Meeting",
                [ActivityCategoryCode.Treatment] = "Treatment",
                [ActivityCategoryCode.TreatmentTasks] = "Treatment Tasks"
            };


    }
}
