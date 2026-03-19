// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
namespace AriaAPI.API.SearchHelpers
{
    public static partial class SearchTypes
    {
        /// <summary>
        /// FHIR R4 status values for the <c>Coverage.status</c> search parameter.
        /// </summary>
        public enum CoverageStatus
        {
            /// <summary>active</summary>
            Active,
            /// <summary>cancelled</summary>
            Cancelled,
            /// <summary>draft</summary>
            Draft,
            /// <summary>entered-in-error</summary>
            EnteredInError
        }

        /// <summary>
        /// Maps <see cref="CoverageStatus"/> to the token string expected by the FHIR server.
        /// </summary>
        public static string CoverageStatusToToken(CoverageStatus s) => s switch
        {
            CoverageStatus.Active => "active",
            CoverageStatus.Cancelled => "cancelled",
            CoverageStatus.Draft => "draft",
            CoverageStatus.EnteredInError => "entered-in-error",
            _ => s.ToString().ToLowerInvariant()
        };
    }
}
