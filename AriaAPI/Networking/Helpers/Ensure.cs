// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace AriaAPI.Networking.Helpers
{
    /// <summary>Lightweight argument validation helpers used by the client and builder.</summary>
    internal static class Ensure
    {
        [return: NotNull]
        public static string NotNullOrWhiteSpace(
            string? value,
            [CallerArgumentExpression("value")] string? paramName = null)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value cannot be null or whitespace.", paramName);
            return value;
        }

        public static void Required(string? value, string name, Type owner)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException($"Option '{name}' is required for {owner.Name}.");
        }

        public static void Required(int? value, string name, Type owner)
        {
            if (value is null || value <= 0)
                throw new InvalidOperationException(
                    $"Option '{name}' must be > 0 for {owner.Name} (actual: {value?.ToString() ?? "null"}).");
        }
    }
}