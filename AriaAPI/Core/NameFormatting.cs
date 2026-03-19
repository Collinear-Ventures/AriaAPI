// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AriaAPI.Core
{
    /// <summary>
    /// Provides methods for converting FHIR-style name strings (typically "LAST, FIRST" format)
    /// into properly cased display names with normalized suffixes and credentials.
    /// </summary>
    public static class NameFormatting
    {
        // Common generational suffixes (roman numerals handled separately).
        private static readonly HashSet<string> KnownSuffixes =
            new(StringComparer.OrdinalIgnoreCase)
            {
            "JR", "SR"
            };

        // Canonical casing for common credentials.
        // Add anything you see in your data.
        private static readonly Dictionary<string, string> CredentialCasing =
            new(StringComparer.OrdinalIgnoreCase)
            {
            { "MD", "MD" },
            { "M.D", "MD" },
            { "DO", "DO" },
            { "D.O", "DO" },
            { "PHD", "PhD" },
            { "PH.D", "PhD" },
            { "MS", "MS" },
            { "M.S", "MS" },
            { "MBA", "MBA" },
            { "MPH", "MPH" },
            { "RN", "RN" },
            { "DABR", "DABR" },
            { "DDS", "DDS" },
            { "DMD", "DMD" },
            { "PA", "PA" },
            { "NP", "NP" },
            { "APRN", "APRN" }
            };

        /// <summary>
        /// Converts "LAST, FIRST" (optionally with suffix/credentials) into "First Last, Suffix, CREDENTIALS".
        /// Examples:
        /// "SMITH, JOHN" -> "John Smith"
        /// "SMITH, JOHN, JR, MD" -> "John Smith, Jr., MD"
        /// "SMITH, JOHN JR MD" -> "John Smith, Jr., MD"
        /// </summary>
        public static string ToTitleCaseFirstLastWithSuffixes(string? input, CultureInfo? culture = null)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            culture ??= CultureInfo.CurrentCulture;
            var textInfo = culture.TextInfo;

            var s = NormalizeWhitespace(input.Trim());

            // Split on commas for the common "LAST, FIRST, SUFFIX, CREDENTIALS" pattern.
            var parts = s.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            // If no comma, just title-case the full string (still tries to format embedded suffix/creds).
            if (parts.Length == 1)
            {
                return FormatNonCommaName(parts[0], textInfo);
            }

            var lastRaw = parts[0];
            var firstRaw = parts.Length > 1 ? parts[1] : string.Empty;

            // Anything after the second comma is treated as suffix/credentials segments.
            var extraSegments = parts.Skip(2).ToList();

            // Also extract suffix/credentials that may be embedded at end of firstRaw (e.g. "JOHN Q JR MD").
            var embeddedPostNominals = ExtractTrailingPostNominals(ref firstRaw);

            // Combine postnominals from both sources.
            var postNominals = new List<string>();
            postNominals.AddRange(embeddedPostNominals);
            foreach (var seg in extraSegments)
                postNominals.AddRange(SplitPostNominals(seg));

            // Title case the name parts (lower first to handle ALL CAPS). [1](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.textinfo.totitlecase?view=net-10.0)[2](https://stackoverflow.com/questions/2697203/textinfo-totitlecase-does-not-work-as-expected-for-all-caps-strings)
            var firstTc = ToPersonTitleCase(firstRaw, textInfo);
            var lastTc = ToPersonTitleCase(lastRaw, textInfo);

            var name = $"{firstTc} {lastTc}".Trim();

            // Normalize suffix/credentials tokens.
            var normalizedPostNominals = postNominals
                .Select(NormalizePostNominalToken)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return normalizedPostNominals.Count == 0
                ? name
                : $"{name}, {string.Join(", ", normalizedPostNominals)}";
        }

        private static string FormatNonCommaName(string value, TextInfo textInfo)
        {
            // If you want: detect "First Last, MD" even without a comma,
            // you can extend this, but for now we just title-case the whole thing.
            return ToPersonTitleCase(value, textInfo);
        }

        private static string ToPersonTitleCase(string value, TextInfo textInfo)
        {
            var v = NormalizeWhitespace(value);

            // ToTitleCase won't convert ALL CAPS words; lower first. [1](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.textinfo.totitlecase?view=net-10.0)[2](https://stackoverflow.com/questions/2697203/textinfo-totitlecase-does-not-work-as-expected-for-all-caps-strings)
            var title = textInfo.ToTitleCase(v.ToLower());

            // Fix common punctuation capitalization: O'Brien, Doe-Smith, etc. [3](https://hapifhir.io/hapi-fhir/docs/model/references.html)
            var chars = title.ToCharArray();
            for (int i = 0; i + 1 < chars.Length; i++)
            {
                if (chars[i] == '\'' || chars[i] == '-')
                    chars[i + 1] = char.ToUpper(chars[i + 1], CultureInfo.CurrentCulture);
            }

            return new string(chars);
        }

        private static string NormalizeWhitespace(string s)
            => string.Join(" ", s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));


        private static List<string> ExtractTrailingPostNominals(ref string firstPart)
        {
            // Pull trailing tokens from firstPart ONLY if they are clearly suffix/credentials.
            // e.g. "JOHN Q JR MD" -> firstPart becomes "JOHN Q", returns ["JR","MD"]
            var tokens = firstPart.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
            var trailing = new List<string>();

            while (tokens.Count > 0)
            {
                var rawToken = tokens[^1];
                var core = StripPunctuation(rawToken);

                // ✅ Only strip if we are confident this is a suffix/credential
                bool isKnownCredential = CredentialCasing.ContainsKey(core);
                bool isSuffix = KnownSuffixes.Contains(core);
                bool isRoman = IsRomanNumeral(core);

                if (isKnownCredential || isSuffix || isRoman)
                {
                    trailing.Insert(0, rawToken);
                    tokens.RemoveAt(tokens.Count - 1);
                }
                else
                {
                    break;
                }
            }

            firstPart = string.Join(" ", tokens);
            return trailing;
        }

        private static IEnumerable<string> SplitPostNominals(string segment)
        {
            // Allow "MD PhD" inside one comma segment.
            return segment.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }

        private static string NormalizePostNominalToken(string token)
        {
            var raw = token.Trim();
            if (raw.Length == 0) return string.Empty;

            // Remove trailing commas/periods for classification, but keep meaning.
            var core = StripPunctuation(raw);

            // Roman numerals: II, III, IV, etc.
            if (IsRomanNumeral(core))
                return core.ToUpperInvariant();

            // Jr/Sr -> "Jr." / "Sr."
            if (KnownSuffixes.Contains(core))
            {
                var t = char.ToUpperInvariant(core[0]) + core.Substring(1).ToLowerInvariant();
                return t.EndsWith('.') ? t : $"{t}.";
            }

            // Known credentials: MD/DO/PhD/etc.
            if (CredentialCasing.TryGetValue(core, out var canonical))
                return canonical;

            // Heuristic: short all-letter tokens (2-6) are likely credentials => uppercase.
            if (LooksLikeCredential(core))
                return core.ToUpperInvariant();

            // Otherwise: return as-is (or title-case if you prefer).
            return core;
        }

        private static string StripPunctuation(string s)
            => s.Trim().TrimEnd('.', ',').Replace(".", "");

        private static bool LooksLikeCredential(string core)
            => core.Length is >= 2 and <= 6 && core.All(char.IsLetter);

        private static bool IsRomanNumeral(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = s.ToUpperInvariant();
            // Simple roman numeral check for common suffix forms.
            return s.All(c => "IVXLCDM".Contains(c)) &&
                   (s.Length <= 6); // keep it conservative
        }
    }
}