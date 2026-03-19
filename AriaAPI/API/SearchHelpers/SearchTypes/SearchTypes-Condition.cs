// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AriaAPI.API.SingleResourceSearch.ConditionSearch;

namespace AriaAPI.API.SearchHelpers
{
    public static partial class SearchTypes
    {
        /// <summary>
        /// Enumerates supported clinical statuses for the <c>clinical-status</c> token search.
        /// </summary>
        public enum ClinicalStatus
        {
            /// <summary>active</summary>
            Active,
            /// <summary>inactive</summary>
            Inactive,
            /// <summary>resolved</summary>
            Resolved,
            /// <summary>subsided</summary>
            Subsided,
            /// <summary>controlled</summary>
            Controlled,
            /// <summary>progressed</summary>
            Progressed,
            /// <summary>other</summary>
            Other,
            /// <summary>ruled-out</summary>
            RuledOut,
            /// <summary>cured</summary>
            Cured,
            /// <summary>in-remission</summary>
            InRemission
        }

        /// <summary>
        /// Enumerates verification statuses for <c>verification-status</c>.
        /// </summary>
        public enum VerificationState
        {
            /// <summary>provisional</summary>
            Provisional,
            /// <summary>confirmed</summary>
            Confirmed,
            /// <summary>entered-in-error</summary>
            EnteredInError
        }

        /// <summary>
        /// Maps <see cref="ClinicalStatus"/> to the token value expected by the search parameter.
        /// </summary>
        public static string ClinicalStatusToToken(ClinicalStatus status) => status switch
        {
            ClinicalStatus.Active => "active",
            ClinicalStatus.Inactive => "inactive",
            ClinicalStatus.Resolved => "resolved",
            ClinicalStatus.Subsided => "subsided",
            ClinicalStatus.Controlled => "controlled",
            ClinicalStatus.Progressed => "progressed",
            ClinicalStatus.Other => "other",
            ClinicalStatus.RuledOut => "ruled-out",
            ClinicalStatus.Cured => "cured",
            ClinicalStatus.InRemission => "in-remission",
            _ => status.ToString().ToLowerInvariant()
        };

        /// <summary>
        /// Maps <see cref="VerificationState"/> to the token value expected by the search parameter.
        /// </summary>
        public static string VerificationStatusToToken(VerificationState state) => state switch
        {
            VerificationState.Provisional => "provisional",
            VerificationState.Confirmed => "confirmed",
            VerificationState.EnteredInError => "entered-in-error",
            _ => state.ToString().ToLowerInvariant()
        };
    }
}
