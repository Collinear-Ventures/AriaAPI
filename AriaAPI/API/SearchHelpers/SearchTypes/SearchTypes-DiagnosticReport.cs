// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
namespace AriaAPI.API.SearchHelpers
{
    public static partial class SearchTypes
    {
        /// <summary>
        /// FHIR R4 status values for the <c>DiagnosticReport.status</c> search parameter.
        /// </summary>
        public enum DiagnosticReportStatus
        {
            /// <summary>registered</summary>
            Registered,
            /// <summary>partial</summary>
            Partial,
            /// <summary>preliminary</summary>
            Preliminary,
            /// <summary>final</summary>
            Final,
            /// <summary>amended</summary>
            Amended,
            /// <summary>corrected</summary>
            Corrected,
            /// <summary>appended</summary>
            Appended,
            /// <summary>cancelled</summary>
            Cancelled,
            /// <summary>entered-in-error</summary>
            EnteredInError,
            /// <summary>unknown</summary>
            Unknown
        }

        /// <summary>
        /// Maps <see cref="DiagnosticReportStatus"/> to the token string expected by the FHIR server.
        /// </summary>
        public static string DiagnosticReportStatusToToken(DiagnosticReportStatus s) => s switch
        {
            DiagnosticReportStatus.Registered => "registered",
            DiagnosticReportStatus.Partial => "partial",
            DiagnosticReportStatus.Preliminary => "preliminary",
            DiagnosticReportStatus.Final => "final",
            DiagnosticReportStatus.Amended => "amended",
            DiagnosticReportStatus.Corrected => "corrected",
            DiagnosticReportStatus.Appended => "appended",
            DiagnosticReportStatus.Cancelled => "cancelled",
            DiagnosticReportStatus.EnteredInError => "entered-in-error",
            DiagnosticReportStatus.Unknown => "unknown",
            _ => s.ToString().ToLowerInvariant()
        };
    }
}
