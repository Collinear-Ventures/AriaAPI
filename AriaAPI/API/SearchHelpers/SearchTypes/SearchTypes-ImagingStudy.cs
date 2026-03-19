// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
namespace AriaAPI.API.SearchHelpers
{
    public static partial class SearchTypes
    {
        /// <summary>
        /// FHIR R4 status values for the <c>ImagingStudy.status</c> search parameter.
        /// </summary>
        public enum ImagingStudyStatus
        {
            /// <summary>registered</summary>
            Registered,
            /// <summary>available</summary>
            Available,
            /// <summary>cancelled</summary>
            Cancelled,
            /// <summary>entered-in-error</summary>
            EnteredInError,
            /// <summary>unknown</summary>
            Unknown
        }

        /// <summary>
        /// Maps <see cref="ImagingStudyStatus"/> to the token string expected by the FHIR server.
        /// </summary>
        public static string ImagingStudyStatusToToken(ImagingStudyStatus s) => s switch
        {
            ImagingStudyStatus.Registered => "registered",
            ImagingStudyStatus.Available => "available",
            ImagingStudyStatus.Cancelled => "cancelled",
            ImagingStudyStatus.EnteredInError => "entered-in-error",
            ImagingStudyStatus.Unknown => "unknown",
            _ => s.ToString().ToLowerInvariant()
        };
    }
}
