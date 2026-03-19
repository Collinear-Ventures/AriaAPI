// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.API.SearchHelpers;
using AriaAPI.Core;
using AriaAPI.Networking.Core;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AriaAPI.API.SingleResourceSearch
{

    /// <summary>
    /// Static helper for Patient queries that follows the same pattern as AppointmentsByDateAndCategoriesAsync:
    /// - Accepts a ClientConfigurator
    /// - Builds SearchParams via Builder&lt;Patient&gt;
    /// - Calls resourceClient.AggregateResourcesAsync(searchParams) to page across results
    /// </summary>
    public static class PatientSearch
    {
        // -----------------------------
        // Parameter types
        // -----------------------------

        /// <summary>
        /// Represents a single _has reverse-chaining constraint:
        /// produces a key like "_has:{Target}:{Reference}:{Param}" with a value.
        /// Examples:
        ///   Target="Appointment", Reference="patient", Param="date", Value="2015-10-08"
        ///   Target="CareTeam",    Reference="patient", Param="participant", Value="Organization/Organization-Dept-1"
        ///   Target="Flag",        Reference="patient", Param="code", Value="1"
        /// </summary>
        public sealed record HasFilter(
            string Target,    // "Appointment" | "CareTeam" | "Flag"
            string Reference, // typically "patient"
            string Param,     // e.g., "date", "participant", "code"
            string Value      // raw value (e.g., "system|code" or "2015-10-08")
        );

        /// <summary>
        /// Identifier groups to support AND-of-OR semantics:
        /// - Each inner list is OR (comma-joined in one "identifier" key).
        /// - Multiple groups are AND (multiple "identifier" keys).
        /// </summary>
        public sealed class IdentifierGroups : List<List<string>> { }

        /// <summary>
        /// General-purpose input parameters for Patient searches.
        /// </summary>
        public sealed class PatientSearchParams
        {
            /// <summary>FHIR _id (logical id)</summary>
            public string? Id { get; init; }

            /// <summary>
            /// Active patients only. When <c>null</c> (default), the query sends <c>active=true</c>,
            /// filtering to active patients only. Set to <c>false</c> explicitly to include inactive patients.
            /// </summary>
            public bool? Active { get; init; }

            /// <summary>FHIR birthdate parameter, e.g., "eq2011-05-23", "ge1970-01-01".</summary>
            public string? BirthDate { get; init; }

            /// <summary>
            /// AND-of-OR identifiers (see IdentifierGroups). Example:
            /// AND[
            ///   OR["http://varian.com/fhir/identifier/Patient/ARIAID1|ID1",
            ///      "http://varian.com/fhir/identifier/Patient/ARIAID2|ID2"],
            ///   OR["http://varian.com/fhir/identifier/Patient/MRN|MRN001"]
            /// ]
            /// </summary>
            public IdentifierGroups? Identifiers { get; init; }

            /// <summary>Free-text search across PatientId1 | PatientId2 | LastName | FirstName.</summary>
            public string? NameOrIdentifier { get; init; }

            /// <summary>Search test patients only (vendor-specific). If null, omit.</summary>
            public bool? TestPatient { get; init; }

            /// <summary>Reverse chaining constraints (_has:Appointment|CareTeam|Flag ...).</summary>
            public List<HasFilter>? Has { get; init; }
        }

        // -----------------------------
        // Public API (AppointmentsByDateAndCategoriesAsync-style)
        // -----------------------------

        /// <summary>
        /// Returns a single Patient (first match) using the general PatientSearchParams.
        /// Applies active=true by default unless overridden.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="p">Search parameter bag.</param>
        /// <param name="listReturnLimit">Maximum number of results for the list call.</param>
        /// <param name="ct">Cancellation token.</param>
        public static async Task<Patient?> PatientAsync(
            ClientConfigurator configurator,
            PatientSearchParams p,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            // Reuse list call with cap = 1
            var list = await PatientsAsync(configurator, p, listReturnLimit: 1, ct: ct).ConfigureAwait(false);
            return list.FirstOrDefault();
        }

        /// <summary>
        /// Returns a single Patient (first match) using the identifier.
        /// Applies active=true by default unless overridden.
        /// </summary>
        /// <param name="configurator">ClientConfigurator for resource-specific client creation.</param>
        /// <param name="_id">Logical ID of the patient resource.</param>
        /// <param name="listReturnLimit">Maximum number of results for the list call.</param>
        /// <param name="ct">Cancellation token.</param>
        public static async Task<Patient?> PatientAsync(
            ClientConfigurator configurator,
            string _id,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            ValidateIdentifierInput(_id, nameof(_id));
            // Reuse list call with cap = 1
            var list = await PatientsAsync(configurator, new PatientSearchParams() { Id = _id }, listReturnLimit: 1, ct: ct).ConfigureAwait(false);
            return list.FirstOrDefault();
        }

        /// <summary>
        /// Returns patients matching the provided <see cref="PatientSearchParams"/>.
        /// Uses <see cref="ClientConfigurator"/> and aggregates across pages.
        /// </summary>
        /// <param name="configurator">Client configurator providing a FHIR resource client and auth.</param>
        /// <param name="p">Search parameter bag; see <see cref="PatientSearchParams"/> for details.</param>
        /// <param name="listReturnLimit">
        /// Final defensive cap on the number of returned results. Values &lt;= 0 are treated as unbounded.
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A deduplicated list of <see cref="Patient"/> resources matching the criteria.</returns>
        /// <remarks>
        /// <para>
        /// <see cref="PatientSearchParams.Identifiers"/> uses AND-of-OR semantics: each inner list is
        /// comma-joined (OR within a group), and multiple groups are fanned out into separate FHIR queries
        /// via <see cref="FanOutSearchHelper"/>. The fan-out helper intersects results across queries,
        /// preserving AND semantics across groups while avoiding repeated <c>identifier</c> keys that the
        /// Aria FHIR server rejects.
        /// </para>
        /// <para>
        /// <c>_has</c> reverse-chaining filters each produce a unique key (e.g.,
        /// <c>_has:Appointment:patient:date</c>) and are folded into the base builder as scalar parameters.
        /// </para>
        /// </remarks>
        public static async Task<List<Patient>> PatientsAsync(
            ClientConfigurator configurator,
            PatientSearchParams p,
            int listReturnLimit = SearchExecutor.DefaultServerMaxResults,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(configurator);
            p ??= new PatientSearchParams();

            var limit = SearchExecutor.NormalizeLimit(listReturnLimit);

            // Validate _has filters up front
            if (p.Has is { Count: > 0 })
            {
                foreach (var has in p.Has!)
                {
                    if (string.IsNullOrWhiteSpace(has.Target))
                        throw new ArgumentException("HasFilter.Target must not be null or empty.");
                    if (string.IsNullOrWhiteSpace(has.Reference))
                        throw new ArgumentException($"HasFilter.Reference must not be null or empty (Target='{has.Target}').");
                    if (string.IsNullOrWhiteSpace(has.Param))
                        throw new ArgumentException($"HasFilter.Param must not be null or empty (Target='{has.Target}', Reference='{has.Reference}').");
                    if (string.IsNullOrWhiteSpace(has.Value))
                        throw new ArgumentException($"HasFilter.Value must not be null or empty (Target='{has.Target}', Reference='{has.Reference}', Param='{has.Param}').");
                }
            }

            // Validate free-text inputs to guard against oversized or malformed query strings
            ValidateIdentifierInput(p.Id, nameof(p.Id));
            ValidateIdentifierInput(p.NameOrIdentifier, nameof(p.NameOrIdentifier));
            if (p.Identifiers is { Count: > 0 })
            {
                foreach (var orGroup in p.Identifiers)
                {
                    if (orGroup is null) continue;
                    foreach (var identifier in orGroup)
                        ValidateIdentifierInput(identifier, "Identifiers");
                }
            }

            Builder<Patient> MakeBaseBuilder()
            {
                var builder = new Builder<Patient>();

                if (!string.IsNullOrWhiteSpace(p.Id))
                    builder.ById(p.Id!);

                var active = p.Active ?? true;
                builder.With("active", active ? "true" : "false");

                if (!string.IsNullOrWhiteSpace(p.BirthDate))
                    builder.With("birthdate", p.BirthDate!);
                if (!string.IsNullOrWhiteSpace(p.NameOrIdentifier))
                    builder.With("name-or-identifier", p.NameOrIdentifier!);
                if (p.TestPatient is bool tp)
                    builder.With("testPatient", tp ? "true" : "false");

                // _has reverse chaining (each produces a unique key, not repeated keys)
                if (p.Has is { Count: > 0 })
                {
                    foreach (var has in p.Has!)
                    {
                        var key = $"_has:{has.Target}:{has.Reference}:{has.Param}";
                        builder.With(key, has.Value);
                    }
                }

                if (limit != int.MaxValue)
                    builder.WithCount(limit);
                return builder;
            }

            // identifier AND-of-OR: each OR group is comma-joined into one value.
            // Multiple groups become multiple values in one FanOutParam. The fan-out
            // helper issues a separate query per value (each group), then intersects
            // results across queries — preserving AND semantics across groups.
            var fanOuts = new List<FanOutSearchHelper.FanOutParam>();
            if (p.Identifiers is { Count: > 0 })
            {
                var identifierValues = new List<string>();
                foreach (var orList in p.Identifiers!)
                {
                    if (orList is { Count: > 0 })
                    {
                        var joined = string.Join(",", orList.Where(s => !string.IsNullOrWhiteSpace(s)));
                        if (!string.IsNullOrWhiteSpace(joined))
                            identifierValues.Add(joined);
                    }
                }
                if (identifierValues.Count > 0)
                    fanOuts.Add(new FanOutSearchHelper.FanOutParam("identifier", identifierValues));
            }

            return await SearchExecutor.ExecuteAsync(
                configurator,
                MakeBaseBuilder,
                fanOuts,
                limit,
                ct).ConfigureAwait(false);
        }

        // -----------------------------
        // Convenience factory helpers for common _has shapes
        // -----------------------------

        /// <summary>Returns a HasFilter for appointments on the given date.</summary>
        public static HasFilter HasAppointmentOnDate(string yyyyMmDd)
            => new("Appointment", "patient", "date", yyyyMmDd);

        /// <summary>Returns a HasFilter for CareTeam with the given department organization reference.</summary>
        public static HasFilter HasCareTeamDepartment(string organizationRef)
            => new("CareTeam", "patient", "participant", organizationRef); // e.g., "Organization/Organization-Dept-1"

        /// <summary>Returns a HasFilter for CareTeam with the given practitioner reference.</summary>
        public static HasFilter HasCareTeamPractitioner(string practitionerRef)
            => new("CareTeam", "patient", "participant", practitionerRef); // e.g., "Practitioner/Practitioner-1004"

        /// <summary>Returns a HasFilter for flags with the given code or system|code.</summary>
        public static HasFilter HasFlagCode(string codeOrSystemPipeCode)
            => new("Flag", "patient", "code", codeOrSystemPipeCode); // e.g., "1" or "http://system|code"

        // Compiled once; allows standard FHIR identifier tokens (e.g., "http://system|value") and
        // FHIR R4 logical ids, while blocking control characters and obvious injection strings.
        private static readonly System.Text.RegularExpressions.Regex _identifierPattern =
            new System.Text.RegularExpressions.Regex(
                @"^[A-Za-z0-9\-_.|:/# ]*$",
                System.Text.RegularExpressions.RegexOptions.Compiled);

        /// <summary>
        /// Validates a single patient search input string.
        /// Accepts FHIR logical ids, name tokens, and system-qualified identifier values (system|value).
        /// </summary>
        /// <param name="value">The value to validate; <c>null</c> is always accepted.</param>
        /// <param name="paramName">Parameter name used in the exception message.</param>
        /// <exception cref="ArgumentException">Thrown when the value is too long or contains invalid characters.</exception>
        internal static void ValidateIdentifierInput(string? value, string paramName)
        {
            if (value is null) return;
            if (value.Length > 200)
                throw new ArgumentException($"'{paramName}' must not exceed 200 characters.", paramName);
            if (!_identifierPattern.IsMatch(value))
                throw new ArgumentException(
                    $"'{paramName}' contains invalid characters. Allowed: letters, digits, and the characters - _ . | : / # and space.",
                    paramName);
        }


    }

}
