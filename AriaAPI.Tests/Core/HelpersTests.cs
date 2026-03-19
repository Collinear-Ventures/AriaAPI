// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.Core;
using Xunit;

namespace AriaAPI.Tests.Core
{
    /// <summary>
    /// Tests for <see cref="AriaFhirClientHelpers.EscapeCsv"/>.
    /// </summary>
    public sealed class HelpersTests
    {
        /// <summary>Null input returns an empty string.</summary>
        [Fact]
        public void EscapeCsv_NullInput_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, AriaFhirClientHelpers.EscapeCsv(null));
        }

        /// <summary>Empty string input returns an empty string.</summary>
        [Fact]
        public void EscapeCsv_EmptyString_ReturnsEmpty()
        {
            Assert.Equal(string.Empty, AriaFhirClientHelpers.EscapeCsv(""));
        }

        /// <summary>A value containing a comma is wrapped in double-quotes.</summary>
        [Fact]
        public void EscapeCsv_CommaInValue_WrapsInQuotes()
        {
            Assert.Equal("\"a,b\"", AriaFhirClientHelpers.EscapeCsv("a,b"));
        }

        /// <summary>A value containing a double-quote has quotes doubled and is wrapped in double-quotes.</summary>
        [Fact]
        public void EscapeCsv_QuoteInValue_DoublesQuote()
        {
            Assert.Equal("\"a\"\"b\"", AriaFhirClientHelpers.EscapeCsv("a\"b"));
        }

        /// <summary>A clean value with no special characters passes through unchanged.</summary>
        [Fact]
        public void EscapeCsv_CleanValue_PassesThrough()
        {
            Assert.Equal("hello", AriaFhirClientHelpers.EscapeCsv("hello"));
        }
    }
}
