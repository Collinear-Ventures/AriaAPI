// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using AriaAPI.Networking.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AriaAPI.Networking.Helpers
{
    /// <summary>
    /// Extension helpers for <see cref="FhirAction"/>.
    /// </summary>
    internal static class FhirActionExtensions
    {
        /// <summary>
        /// Convert <see cref="FhirAction"/> to the string code expected by resource constructors.
        /// </summary>
        /// <param name="action">The <see cref="FhirAction"/> to convert.</param>
        /// <returns>The string code used by resource constructors (for example, "read").</returns>
        public static string ToCode(this FhirAction action) =>
            action switch
            {
                FhirAction.Read => "read",
                FhirAction.Search => "search",
                FhirAction.Update => "update",
                FhirAction.Delete => "delete",
                FhirAction.Create => "create",
                FhirAction.Expand => "$expand",
                FhirAction.MarkAsExported => "markAsExported",
                FhirAction.CheckIn => "$checkin",
                FhirAction.CheckOut => "$checkout",
                _ => throw new ArgumentOutOfRangeException(nameof(action), $"Unsupported FhirAction value: {action}.")
            };
    }

}
