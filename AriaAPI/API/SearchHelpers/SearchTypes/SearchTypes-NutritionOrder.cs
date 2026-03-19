// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
namespace AriaAPI.API.SearchHelpers
{
    public static partial class SearchTypes
    {
        /// <summary>
        /// FHIR R4 status values for the <c>NutritionOrder.status</c> search parameter.
        /// </summary>
        public enum NutritionOrderStatus
        {
            /// <summary>draft</summary>
            Draft,
            /// <summary>active</summary>
            Active,
            /// <summary>on-hold</summary>
            OnHold,
            /// <summary>revoked</summary>
            Revoked,
            /// <summary>completed</summary>
            Completed,
            /// <summary>entered-in-error</summary>
            EnteredInError,
            /// <summary>unknown</summary>
            Unknown
        }

        /// <summary>
        /// Maps <see cref="NutritionOrderStatus"/> to the token string expected by the FHIR server.
        /// </summary>
        public static string NutritionOrderStatusToToken(NutritionOrderStatus s) => s switch
        {
            NutritionOrderStatus.Draft => "draft",
            NutritionOrderStatus.Active => "active",
            NutritionOrderStatus.OnHold => "on-hold",
            NutritionOrderStatus.Revoked => "revoked",
            NutritionOrderStatus.Completed => "completed",
            NutritionOrderStatus.EnteredInError => "entered-in-error",
            NutritionOrderStatus.Unknown => "unknown",
            _ => s.ToString().ToLowerInvariant()
        };
    }
}
