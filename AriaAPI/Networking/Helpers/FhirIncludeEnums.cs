// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.

using System;
using System.Collections.Generic;
using Hl7.Fhir.Rest;

namespace AriaAPI.Resources.Includes
{

    /// <summary>
    /// Holds the resource name and segment mappings for a single include enum type.
    /// </summary>
    public sealed record FhirIncludeInfo(string Resource, IReadOnlyDictionary<Enum, string> Segments);

    /// <summary>
    /// Single registry that maps each include enum type to its resource name and segment strings.
    /// </summary>
    public static class FhirIncludeRegistry
    {
        private static readonly IReadOnlyDictionary<Type, FhirIncludeInfo> _registry =
            new Dictionary<Type, FhirIncludeInfo>
            {
                [typeof(PatientInclude)] = new("Patient", new Dictionary<Enum, string>
                {
                    [PatientInclude.ManagingOrganization] = "managing-organization",
                }),
                [typeof(ConditionInclude)] = new("Condition", new Dictionary<Enum, string>
                {
                    [ConditionInclude.Patient] = "patient",
                    [ConditionInclude.Recorder] = "recorder",
                }),
                [typeof(DocumentReferenceInclude)] = new("DocumentReference", new Dictionary<Enum, string>
                {
                    [DocumentReferenceInclude.Authenticator] = "authenticator",
                    [DocumentReferenceInclude.Custodian] = "custodian",
                    [DocumentReferenceInclude.Author] = "author",
                    [DocumentReferenceInclude.Content] = "content",
                    [DocumentReferenceInclude.Patient] = "patient",
                    [DocumentReferenceInclude.Signer] = "signer",
                    [DocumentReferenceInclude.Subject] = "subject",
                    [DocumentReferenceInclude.Supervisor] = "supervisor",
                }),
                [typeof(ObservationInclude)] = new("Observation", new Dictionary<Enum, string>
                {
                    [ObservationInclude.BasedOn] = "based-on",
                    [ObservationInclude.Patient] = "patient",
                    [ObservationInclude.Performer] = "performer",
                }),
                [typeof(OrganizationInclude)] = new("Organization", new Dictionary<Enum, string>
                {
                    [OrganizationInclude.PartOf] = "partof",
                }),
                [typeof(LocationInclude)] = new("Location", new Dictionary<Enum, string>
                {
                    [LocationInclude.ServiceOrganization] = "service-organization",
                }),
                [typeof(HealthcareServiceInclude)] = new("HealthcareService", new Dictionary<Enum, string>
                {
                    [HealthcareServiceInclude.Organization] = "organization",
                }),
                [typeof(GroupInclude)] = new("Group", new Dictionary<Enum, string>
                {
                    [GroupInclude.Member] = "member",
                }),
                [typeof(ChargeItemInclude)] = new("ChargeItem", new Dictionary<Enum, string>
                {
                    [ChargeItemInclude.Account] = "account",
                    [ChargeItemInclude.Patient] = "patient",
                    [ChargeItemInclude.SupportingInformation] = "supportingInformation",
                }),
                [typeof(CarePlanInclude)] = new("CarePlan", new Dictionary<Enum, string>
                {
                    [CarePlanInclude.ActivityOutcomeReference] = "activity-outcome-reference",
                    [CarePlanInclude.ActivityReference] = "activity-reference",
                    [CarePlanInclude.Author] = "author",
                    [CarePlanInclude.Patient] = "patient",
                }),
                [typeof(AllergyIntoleranceInclude)] = new("AllergyIntolerance", new Dictionary<Enum, string>
                {
                    [AllergyIntoleranceInclude.Patient] = "patient",
                    [AllergyIntoleranceInclude.Recorder] = "recorder",
                }),
                [typeof(AppointmentInclude)] = new("Appointment", new Dictionary<Enum, string>
                {
                    [AppointmentInclude.Actor] = "actor",
                    [AppointmentInclude.Definition] = "definition",
                    [AppointmentInclude.Patient] = "patient",
                }),
                [typeof(BodyStructureInclude)] = new("BodyStructure", new Dictionary<Enum, string>
                {
                    [BodyStructureInclude.Patient] = "patient",
                }),
                [typeof(DeviceInclude)] = new("Device", new Dictionary<Enum, string>
                {
                    [DeviceInclude.ServiceOrganization] = "service-organization",
                }),
                [typeof(BundleInclude)] = new("Bundle", new Dictionary<Enum, string>
                {
                    [BundleInclude.None] = string.Empty,
                }),
                [typeof(AuditEventInclude)] = new("AuditEvent", new Dictionary<Enum, string>
                {
                    [AuditEventInclude.None] = string.Empty,
                }),
                [typeof(OperationDefinitionInclude)] = new("OperationDefinition", new Dictionary<Enum, string>
                {
                    [OperationDefinitionInclude.None] = string.Empty,
                }),
                [typeof(ActivityDefinitionInclude)] = new("ActivityDefinition", new Dictionary<Enum, string>
                {
                    [ActivityDefinitionInclude.Subject] = "subject",
                }),
                [typeof(CareTeamInclude)] = new("CareTeam", new Dictionary<Enum, string>
                {
                    [CareTeamInclude.Participant] = "participant",
                    [CareTeamInclude.Patient] = "patient",
                }),
                [typeof(PractitionerInclude)] = new("Practitioner", new Dictionary<Enum, string>
                {
                    [PractitionerInclude.ServiceOrganization] = "service-organization",
                }),
                [typeof(ProcedureInclude)] = new("Procedure", new Dictionary<Enum, string>
                {
                    [ProcedureInclude.BasedOn] = "based-on",
                    [ProcedureInclude.Patient] = "patient",
                }),
                [typeof(ServiceRequestInclude)] = new("ServiceRequest", new Dictionary<Enum, string>
                {
                    [ServiceRequestInclude.BasedOn] = "based-on",
                    [ServiceRequestInclude.Patient] = "patient",
                    [ServiceRequestInclude.Requester] = "requester",
                }),
                [typeof(TaskInclude)] = new("Task", new Dictionary<Enum, string>
                {
                    [TaskInclude.BasedOn] = "based-on",
                    [TaskInclude.For] = "for",
                    [TaskInclude.Recipient] = "recipient",
                }),
                [typeof(ValueSetInclude)] = new("ValueSet", new Dictionary<Enum, string>
                {
                    [ValueSetInclude.None] = string.Empty,
                }),
                [typeof(EncounterInclude)] = new("Encounter", new Dictionary<Enum, string>
                {
                    [EncounterInclude.Patient] = "patient",
                    [EncounterInclude.Participant] = "participant",
                    [EncounterInclude.Location] = "location",
                }),
                [typeof(MedicationRequestInclude)] = new("MedicationRequest", new Dictionary<Enum, string>
                {
                    [MedicationRequestInclude.Patient] = "patient",
                    [MedicationRequestInclude.Requester] = "requester",
                    [MedicationRequestInclude.MedicationReference] = "medication",
                }),
                [typeof(MedicationAdministrationInclude)] = new("MedicationAdministration", new Dictionary<Enum, string>
                {
                    [MedicationAdministrationInclude.Patient] = "patient",
                    [MedicationAdministrationInclude.Performer] = "performer",
                }),
                [typeof(DiagnosticReportInclude)] = new("DiagnosticReport", new Dictionary<Enum, string>
                {
                    [DiagnosticReportInclude.Patient] = "patient",
                    [DiagnosticReportInclude.Performer] = "performer",
                    [DiagnosticReportInclude.Result] = "result",
                }),
                [typeof(ImagingStudyInclude)] = new("ImagingStudy", new Dictionary<Enum, string>
                {
                    [ImagingStudyInclude.Patient] = "patient",
                    [ImagingStudyInclude.Performer] = "performer",
                }),
                [typeof(PractitionerRoleInclude)] = new("PractitionerRole", new Dictionary<Enum, string>
                {
                    [PractitionerRoleInclude.Practitioner] = "practitioner",
                    [PractitionerRoleInclude.Location] = "location",
                    [PractitionerRoleInclude.Organization] = "organization",
                }),
                [typeof(RelatedPersonInclude)] = new("RelatedPerson", new Dictionary<Enum, string>
                {
                    [RelatedPersonInclude.Patient] = "patient",
                }),
                [typeof(CoverageInclude)] = new("Coverage", new Dictionary<Enum, string>
                {
                    [CoverageInclude.Patient] = "patient",
                    [CoverageInclude.Payor] = "payor",
                }),
                [typeof(ImmunizationInclude)] = new("Immunization", new Dictionary<Enum, string>
                {
                    [ImmunizationInclude.Patient] = "patient",
                }),
                [typeof(ScheduleInclude)] = new("Schedule", new Dictionary<Enum, string>
                {
                    [ScheduleInclude.Actor] = "actor",
                }),
                [typeof(SlotInclude)] = new("Slot", new Dictionary<Enum, string>
                {
                    [SlotInclude.Schedule] = "schedule",
                }),
                [typeof(NutritionOrderInclude)] = new("NutritionOrder", new Dictionary<Enum, string>
                {
                    [NutritionOrderInclude.Patient] = "patient",
                }),
                [typeof(RiskAssessmentInclude)] = new("RiskAssessment", new Dictionary<Enum, string>
                {
                    [RiskAssessmentInclude.Patient] = "patient",
                    [RiskAssessmentInclude.Condition] = "condition",
                }),
            };

        /// <summary>
        /// Gets the full include info (resource name + segments) for an include enum type.
        /// </summary>
        public static FhirIncludeInfo GetInfo(Type includeEnumType)
        {
            if (!_registry.TryGetValue(includeEnumType, out var info))
                throw new NotSupportedException($"No include mapping registered for enum '{includeEnumType.Name}'.");
            return info;
        }

        /// <summary>
        /// Gets the full include info for an include enum type parameter.
        /// </summary>
        public static FhirIncludeInfo GetInfo<TInclude>() where TInclude : struct, Enum
            => GetInfo(typeof(TInclude));
    }

    /// <summary>
    /// Resolves the FHIR resource name for a given include enum type.
    /// Delegates to <see cref="FhirIncludeRegistry"/>.
    /// </summary>
    public static class FhirIncludeResources
    {
        /// <summary>
        /// Gets the resource type name for an include enum type (e.g., "Patient", "Appointment").
        /// </summary>
        public static string GetResourceNameFor(Type includeEnumType)
            => FhirIncludeRegistry.GetInfo(includeEnumType).Resource;

        /// <summary>
        /// Gets the resource type name for an include enum type parameter.
        /// </summary>
        public static string GetResourceNameFor<TInclude>() where TInclude : struct, Enum
            => FhirIncludeRegistry.GetInfo<TInclude>().Resource;
    }


    /// <summary>
    /// Marker interface to unify all include enums for extension methods.
    /// </summary>
    public interface IFhirIncludeEnum { }

    // --------------------
    // Enums per Resource
    // --------------------

    /// <summary>FHIR <c>_include</c> paths for <c>Patient</c> searches.</summary>
    public enum PatientInclude : byte
    {
        /// <summary>Patient:managing-organization</summary>
        ManagingOrganization
    }

    /// <summary>FHIR <c>_include</c> paths for <c>Condition</c> searches.</summary>
    public enum ConditionInclude : byte
    {
        /// <summary>Condition:patient</summary>
        Patient,
        /// <summary>Condition:recorder</summary>
        Recorder
    }

    /// <summary>FHIR <c>_include</c> paths for <c>DocumentReference</c> searches.</summary>
    public enum DocumentReferenceInclude : byte
    {
        /// <summary>DocumentReference:authenticator</summary>
        Authenticator,
        /// <summary>DocumentReference:custodian</summary>
        Custodian,
        /// <summary>DocumentReference:author</summary>
        Author,
        /// <summary>DocumentReference:content</summary>
        Content,
        /// <summary>DocumentReference:patient</summary>
        Patient,
        /// <summary>DocumentReference:signer</summary>
        Signer,
        /// <summary>DocumentReference:subject</summary>
        Subject,
        /// <summary>DocumentReference:supervisor</summary>
        Supervisor
    }

    /// <summary>FHIR <c>_include</c> paths for <c>Observation</c> searches.</summary>
    public enum ObservationInclude : byte
    {
        /// <summary>Observation:based-on</summary>
        BasedOn,
        /// <summary>Observation:patient</summary>
        Patient,
        /// <summary>Observation:performer</summary>
        Performer
    }

    /// <summary>FHIR <c>_include</c> paths for <c>Organization</c> searches.</summary>
    public enum OrganizationInclude : byte
    {
        /// <summary>Organization:partof</summary>
        PartOf
    }

    /// <summary>FHIR <c>_include</c> paths for <c>Location</c> searches.</summary>
    public enum LocationInclude : byte
    {
        /// <summary>Location:service-organization</summary>
        ServiceOrganization
    }

    /// <summary>FHIR <c>_include</c> paths for <c>HealthcareService</c> searches.</summary>
    public enum HealthcareServiceInclude : byte
    {
        /// <summary>HealthcareService:organization</summary>
        Organization
    }

    /// <summary>FHIR <c>_include</c> paths for <c>Group</c> searches.</summary>
    public enum GroupInclude : byte
    {
        /// <summary>Group:member</summary>
        Member
    }

    /// <summary>FHIR <c>_include</c> paths for <c>ChargeItem</c> searches.</summary>
    public enum ChargeItemInclude : byte
    {
        /// <summary>ChargeItem:account</summary>
        Account,
        /// <summary>ChargeItem:patient</summary>
        Patient,
        /// <summary>ChargeItem:supportingInformation</summary>
        SupportingInformation
    }

    /// <summary>FHIR <c>_include</c> paths for <c>CarePlan</c> searches.</summary>
    public enum CarePlanInclude : byte
    {
        /// <summary>CarePlan:activity-outcome-reference</summary>
        ActivityOutcomeReference,
        /// <summary>CarePlan:activity-reference</summary>
        ActivityReference,
        /// <summary>CarePlan:author</summary>
        Author,
        /// <summary>CarePlan:patient</summary>
        Patient
    }

    /// <summary>FHIR <c>_include</c> paths for <c>AllergyIntolerance</c> searches.</summary>
    public enum AllergyIntoleranceInclude : byte
    {
        /// <summary>AllergyIntolerance:patient</summary>
        Patient,
        /// <summary>AllergyIntolerance:recorder</summary>
        Recorder
    }

    /// <summary>FHIR <c>_include</c> paths for <c>Appointment</c> searches.</summary>
    public enum AppointmentInclude : byte
    {
        /// <summary>Appointment:actor</summary>
        Actor,
        /// <summary>Appointment:definition</summary>
        Definition,
        /// <summary>Appointment:patient</summary>
        Patient
    }

    /// <summary>FHIR <c>_include</c> paths for <c>BodyStructure</c> searches.</summary>
    public enum BodyStructureInclude : byte
    {
        /// <summary>BodyStructure:patient</summary>
        Patient
    }

    /// <summary>FHIR <c>_include</c> paths for <c>Device</c> searches.</summary>
    public enum DeviceInclude : byte
    {
        /// <summary>Device:service-organization</summary>
        ServiceOrganization
    }

    /// <summary>FHIR <c>_include</c> paths for <c>Bundle</c> searches.</summary>
    public enum BundleInclude : byte
    {
        /// <summary>No include (placeholder).</summary>
        None
    }

    /// <summary>FHIR <c>_include</c> paths for <c>AuditEvent</c> searches.</summary>
    public enum AuditEventInclude : byte
    {
        /// <summary>No include (placeholder).</summary>
        None
    }

    /// <summary>FHIR <c>_include</c> paths for <c>OperationDefinition</c> searches.</summary>
    public enum OperationDefinitionInclude : byte
    {
        /// <summary>No include (placeholder).</summary>
        None
    }

    /// <summary>FHIR <c>_include</c> paths for <c>ActivityDefinition</c> searches.</summary>
    public enum ActivityDefinitionInclude : byte
    {
        /// <summary>ActivityDefinition:subject</summary>
        Subject
    }

    /// <summary>FHIR <c>_include</c> paths for <c>CareTeam</c> searches.</summary>
    public enum CareTeamInclude : byte
    {
        /// <summary>CareTeam:participant</summary>
        Participant,
        /// <summary>CareTeam:patient</summary>
        Patient
    }

    /// <summary>FHIR <c>_include</c> paths for <c>Practitioner</c> searches.</summary>
    public enum PractitionerInclude : byte
    {
        /// <summary>Practitioner:service-organization</summary>
        ServiceOrganization
    }

    /// <summary>FHIR <c>_include</c> paths for <c>Procedure</c> searches.</summary>
    public enum ProcedureInclude : byte
    {
        /// <summary>Procedure:based-on</summary>
        BasedOn,
        /// <summary>Procedure:patient</summary>
        Patient
    }

    /// <summary>FHIR <c>_include</c> paths for <c>ServiceRequest</c> searches.</summary>
    public enum ServiceRequestInclude : byte
    {
        /// <summary>ServiceRequest:based-on</summary>
        BasedOn,
        /// <summary>ServiceRequest:patient</summary>
        Patient,
        /// <summary>ServiceRequest:requester</summary>
        Requester
    }

    /// <summary>FHIR <c>_include</c> paths for <c>Task</c> searches.</summary>
    public enum TaskInclude : byte
    {
        /// <summary>Task:based-on</summary>
        BasedOn,
        /// <summary>Task:for</summary>
        For,
        /// <summary>Task:recipient</summary>
        Recipient
    }

    /// <summary>FHIR <c>_include</c> paths for <c>ValueSet</c> searches.</summary>
    public enum ValueSetInclude : byte
    {
        /// <summary>No include (placeholder).</summary>
        None
    }

    /// <summary>FHIR <c>_include</c> paths for <c>Encounter</c> searches.</summary>
    public enum EncounterInclude : byte
    {
        /// <summary>Encounter:patient</summary>
        Patient,
        /// <summary>Encounter:participant</summary>
        Participant,
        /// <summary>Encounter:location</summary>
        Location
    }

    /// <summary>FHIR <c>_include</c> paths for <c>MedicationRequest</c> searches.</summary>
    public enum MedicationRequestInclude : byte
    {
        /// <summary>MedicationRequest:patient</summary>
        Patient,
        /// <summary>MedicationRequest:requester</summary>
        Requester,
        /// <summary>MedicationRequest:medication</summary>
        MedicationReference
    }

    /// <summary>FHIR <c>_include</c> paths for <c>MedicationAdministration</c> searches.</summary>
    public enum MedicationAdministrationInclude : byte
    {
        /// <summary>MedicationAdministration:patient</summary>
        Patient,
        /// <summary>MedicationAdministration:performer</summary>
        Performer
    }

    /// <summary>FHIR <c>_include</c> paths for <c>DiagnosticReport</c> searches.</summary>
    public enum DiagnosticReportInclude : byte
    {
        /// <summary>DiagnosticReport:patient</summary>
        Patient,
        /// <summary>DiagnosticReport:performer</summary>
        Performer,
        /// <summary>DiagnosticReport:result</summary>
        Result
    }

    /// <summary>FHIR <c>_include</c> paths for <c>ImagingStudy</c> searches.</summary>
    public enum ImagingStudyInclude : byte
    {
        /// <summary>ImagingStudy:patient</summary>
        Patient,
        /// <summary>ImagingStudy:performer</summary>
        Performer
    }

    /// <summary>FHIR <c>_include</c> paths for <c>PractitionerRole</c> searches.</summary>
    public enum PractitionerRoleInclude : byte
    {
        /// <summary>PractitionerRole:practitioner</summary>
        Practitioner,
        /// <summary>PractitionerRole:location</summary>
        Location,
        /// <summary>PractitionerRole:organization</summary>
        Organization
    }

    /// <summary>FHIR <c>_include</c> paths for <c>RelatedPerson</c> searches.</summary>
    public enum RelatedPersonInclude : byte
    {
        /// <summary>RelatedPerson:patient</summary>
        Patient
    }

    /// <summary>FHIR <c>_include</c> paths for <c>Coverage</c> searches.</summary>
    public enum CoverageInclude : byte
    {
        /// <summary>Coverage:patient</summary>
        Patient,
        /// <summary>Coverage:payor</summary>
        Payor
    }

    /// <summary>FHIR <c>_include</c> paths for <c>Immunization</c> searches.</summary>
    public enum ImmunizationInclude : byte
    {
        /// <summary>Immunization:patient</summary>
        Patient
    }

    /// <summary>FHIR <c>_include</c> paths for <c>Schedule</c> searches.</summary>
    public enum ScheduleInclude : byte
    {
        /// <summary>Schedule:actor</summary>
        Actor
    }

    /// <summary>FHIR <c>_include</c> paths for <c>Slot</c> searches.</summary>
    public enum SlotInclude : byte
    {
        /// <summary>Slot:schedule</summary>
        Schedule
    }

    /// <summary>FHIR <c>_include</c> paths for <c>NutritionOrder</c> searches.</summary>
    public enum NutritionOrderInclude : byte
    {
        /// <summary>NutritionOrder:patient</summary>
        Patient
    }

    /// <summary>FHIR <c>_include</c> paths for <c>RiskAssessment</c> searches.</summary>
    public enum RiskAssessmentInclude : byte
    {
        /// <summary>RiskAssessment:patient</summary>
        Patient,
        /// <summary>RiskAssessment:condition</summary>
        Condition
    }

    // Make all enums implement marker without affecting public names
    // via type forwarding through a generic shim
    internal static class IncludeEnumShim<T> where T : struct, Enum { }
    // (The marker interface is used by overloaded helpers below.)

    /// <summary>
    /// Maps enums to FHIR _include segment strings.
    /// Delegates to <see cref="FhirIncludeRegistry"/>.
    /// </summary>
    public static class FhirIncludeSegments
    {
        /// <summary>
        /// Converts an include enum value to its FHIR _include segment string.
        /// </summary>
        public static string ToSegment<TInclude>(this TInclude include)
            where TInclude : struct, Enum
        {
            var info = FhirIncludeRegistry.GetInfo<TInclude>();
            var key = (Enum)(object)include;
            return info.Segments[key];
        }
    }

    /// <summary>
    /// Extensions for composing SearchParams with include enums.
    /// </summary>
    public static class SearchParamsIncludeEnumExtensions
    {
        /// <summary>
        /// Adds a single include using an enum value.
        /// </summary>
        public static SearchParams Include<TInclude>(this SearchParams sp, string resourceType, TInclude include)
            where TInclude : struct, Enum
        {
            if (sp == null) throw new ArgumentNullException(nameof(sp));
            if (string.IsNullOrWhiteSpace(resourceType)) throw new ArgumentException("Resource type required", nameof(resourceType));
            var seg = include.ToSegment();
            if (!string.IsNullOrWhiteSpace(seg)) sp.Include($"{resourceType}:{seg}");
            return sp;
        }

        /// <summary>
        /// Adds multiple includes for a given resource type from enum values.
        /// </summary>
        public static SearchParams Include<TInclude>(this SearchParams sp, string resourceType, params TInclude[] includes)
            where TInclude : struct, Enum
        {
            if (includes == null) return sp;
            foreach (var inc in includes)
            {
                sp.Include(resourceType, inc);
            }
            return sp;
        }
    }
}
