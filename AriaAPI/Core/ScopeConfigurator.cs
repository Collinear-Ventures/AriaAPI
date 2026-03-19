// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using AriaAPI.Networking.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AriaAPI.Core
{
    /// <summary>
    /// Indicates the base scope context for a SMART-on-FHIR OAuth2 scope.
    /// </summary>
    public enum ScopeBase
    {
        /// <summary>
        /// Scope applied to the currently authenticated user (user-level).
        /// </summary>
        User,
        /// <summary>
        /// Scope applied at the system level (system-level access).
        /// </summary>
        System
    }

    /// <summary>
    /// Represents the supported FHIR resource scopes used to build OAuth2 scope strings.
    /// Each value corresponds to a FHIR resource type or a logical mapping for scopes.
    /// </summary>
    public enum Scope
    {
        /// <summary>Patient resource scope.</summary>
        Patient,
        /// <summary>Practitioner resource scope.</summary>
        Practitioner,
        /// <summary>Organization resource scope.</summary>
        Organization,
        /// <summary>HealthcareService resource scope.</summary>
        HealthcareService,
        /// <summary>Location resource scope.</summary>
        Location,
        /// <summary>Device resource scope.</summary>
        Device,
        /// <summary>CareTeam resource scope.</summary>
        CareTeam,
        /// <summary>ActivityDefinition resource scope.</summary>
        ActivityDefinition,
        /// <summary>Group resource scope.</summary>
        Group,
        /// <summary>Condition resource scope.</summary>
        Condition,
        /// <summary>AllergyIntolerance resource scope.</summary>
        AllergyIntolerance,
        /// <summary>DocumentReference resource scope.</summary>
        DocumentReference,
        /// <summary>Task resource scope.</summary>
        Task,
        /// <summary>Appointment resource scope.</summary>
        Appointment,
        /// <summary>Observation resource scope.</summary>
        Observation,
        /// <summary>ChargeItem resource scope.</summary>
        ChargeItem,
        /// <summary>CarePlan resource scope.</summary>
        CarePlan,
        /// <summary>BodyStructure resource scope.</summary>
        BodyStructure,
        /// <summary>ValueSet resource scope.</summary>
        ValueSet,
        /// <summary>AuditEvent resource scope.</summary>
        AuditEvent
    }

    /// <summary>
    /// Represents the access type suffix used in scope strings.
    /// 'rs' is used for read/search-like operations, 'cruds' is used for create/update/delete-like operations.
    /// </summary>
    public enum ScopeAccessType
    {
        /// <summary>
        /// Read/search/expand type access (mapped from read/search/$expand).
        /// </summary>
        rs,
        /// <summary>
        /// Create/Read/Update/Delete/Search - used for write-like and other privileged operations.
        /// </summary>
        cruds
    }

    /// <summary>
    /// Provides extension methods to build SMART-on-FHIR style scope strings and to map
    /// resource types and action strings to <see cref="Scope"/> and <see cref="ScopeAccessType"/>.
    /// </summary>
    public static class ScopeExtensions
    {
        /// <summary>
        /// Builds a SMART-on-FHIR scope string from the given base, resource scope, and access type.
        /// </summary>
        /// <param name="scopeBase">The scope base (User or System).</param>
        /// <param name="scope">The FHIR resource scope.</param>
        /// <param name="accessType">The access type suffix (rs or cruds).</param>
        /// <returns>A formatted scope string such as <c>user/Patient.rs</c>.</returns>
        public static string ScopeString(this ScopeBase scopeBase, Scope scope, ScopeAccessType accessType) =>
                   $"{scopeBase.ToString().ToLower()}/{scope.ToString()}.{accessType.ToString()}";

        /// <summary>
        /// Builds a SMART-on-FHIR scope string from the given base, resource type name, and access type.
        /// The resource type name is mapped to a <see cref="Scope"/> enum value.
        /// </summary>
        /// <param name="scopeBase">The scope base (User or System).</param>
        /// <param name="resource">The FHIR resource type name (e.g., "Patient").</param>
        /// <param name="accessType">The access type suffix (rs or cruds).</param>
        /// <returns>A formatted scope string such as <c>system/Patient.cruds</c>.</returns>
        public static string ScopeString(this ScopeBase scopeBase, string resource, ScopeAccessType accessType) =>
            $"{scopeBase.ToString().ToLower()}/{GetScopeFromResource(resource).ToString()}.{accessType.ToString()}";

        /// <summary>
        /// Compose a scope string directly from a <see cref="FhirAction"/> to avoid stringly-typed code.
        /// </summary>
        public static string ScopeString(this ScopeBase scopeBase, string resource, FhirAction action) =>
            scopeBase.ScopeString(resource, action.GetAccessType());

        /// <summary>
        /// Map a FHIR action to the SMART-on-FHIR access type suffix.
        /// </summary>
        public static ScopeAccessType GetAccessType(this FhirAction action) =>
            action switch
            {
                FhirAction.Read or FhirAction.Search or FhirAction.Expand => ScopeAccessType.rs,
                FhirAction.Update or FhirAction.Create or FhirAction.Delete
                or FhirAction.MarkAsExported or FhirAction.CheckIn or FhirAction.CheckOut => ScopeAccessType.cruds,
                _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unsupported FHIR action.")
            };

        /// <summary>
        /// String-based fallback (kept for back-compat). Case-insensitive and fixed casing bug.
        /// </summary>
        public static ScopeAccessType GetAccessTypeFromString(string accessType) =>
            accessType.ToLower() switch
            {
                "read" => ScopeAccessType.rs,
                "search" => ScopeAccessType.rs,
                "$expand" => ScopeAccessType.rs,
                "update" => ScopeAccessType.cruds,
                "create" => ScopeAccessType.cruds,
                "markasexported" => ScopeAccessType.cruds, // <-- fixed casing
                "$checkin" => ScopeAccessType.cruds,
                "$checkout" => ScopeAccessType.cruds,
                "delete" => ScopeAccessType.cruds,
                _ => throw new ArgumentException($"Unsupported access type: {accessType}")
            };

        /// <summary>
        /// Maps a FHIR resource type name to the corresponding <see cref="Scope"/> enum value.
        /// </summary>
        /// <param name="resourceType">The FHIR resource type name (case-insensitive).</param>
        /// <returns>The matching <see cref="Scope"/> value.</returns>
        /// <exception cref="ArgumentException">Thrown when the resource type is not supported.</exception>
        public static Scope GetScopeFromResource(string resourceType) =>
            resourceType.ToLower() switch
            {
                "bundle" => Scope.Patient,
                "patient" => Scope.Patient,
                "practitioner" => Scope.Practitioner,
                "organization" => Scope.Organization,
                "healthcareservice" => Scope.HealthcareService,
                "location" => Scope.Location,
                "device" => Scope.Device,
                "careteam" => Scope.CareTeam,
                "activitydefinition" => Scope.ActivityDefinition,
                "group" => Scope.Group,
                "condition" => Scope.Condition,
                "allergyintolerance" => Scope.AllergyIntolerance,
                "documentreference" => Scope.DocumentReference,
                "task" => Scope.Task,
                "appointment" => Scope.Appointment,
                "observation" => Scope.Observation,
                "chargeitem" => Scope.ChargeItem,
                "careplan" => Scope.CarePlan,
                "bodystructure" => Scope.BodyStructure,
                "valueset" => Scope.ValueSet,
                "auditevent" => Scope.AuditEvent,
                _ => throw new ArgumentException($"Unsupported resource type: {resourceType}")
            };

    }

}


