// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.Core;
using Xunit;

namespace AriaAPI.Tests.Core
{
    /// <summary>
    /// Tests for <see cref="NameFormatting.ToTitleCaseFirstLastWithSuffixes"/>.
    /// All tests are pure unit tests — no infrastructure required.
    /// </summary>
    public sealed class NameFormattingTests
    {
        /// <summary>Null input returns an empty string.</summary>
        [Fact]
        public void NullInput_ReturnsEmpty()
        {
            var result = NameFormatting.ToTitleCaseFirstLastWithSuffixes(null);
            Assert.Equal(string.Empty, result);
        }

        /// <summary>Whitespace-only input returns an empty string.</summary>
        [Fact]
        public void WhitespaceInput_ReturnsEmpty()
        {
            var result = NameFormatting.ToTitleCaseFirstLastWithSuffixes("   ");
            Assert.Equal(string.Empty, result);
        }

        /// <summary>"SMITH, JOHN" is converted to "John Smith".</summary>
        [Fact]
        public void SimpleLastFirst_ReturnsFirstLast()
        {
            var result = NameFormatting.ToTitleCaseFirstLastWithSuffixes("SMITH, JOHN");
            Assert.Equal("John Smith", result);
        }

        /// <summary>All-caps input is title-cased correctly.</summary>
        [Fact]
        public void AllCaps_TitleCased()
        {
            var result = NameFormatting.ToTitleCaseFirstLastWithSuffixes("DOE, JANE");
            Assert.Equal("Jane Doe", result);
        }

        /// <summary>"SMITH, JOHN, JR" is converted to "John Smith, Jr.".</summary>
        [Fact]
        public void WithJrSuffix_FormatsWithPeriod()
        {
            var result = NameFormatting.ToTitleCaseFirstLastWithSuffixes("SMITH, JOHN, JR");
            Assert.Equal("John Smith, Jr.", result);
        }

        /// <summary>"DOE, JANE, SR" is converted to "Jane Doe, Sr.".</summary>
        [Fact]
        public void WithSrSuffix_FormatsWithPeriod()
        {
            var result = NameFormatting.ToTitleCaseFirstLastWithSuffixes("DOE, JANE, SR");
            Assert.Equal("Jane Doe, Sr.", result);
        }

        /// <summary>"SMITH, JOHN, MD" is converted to "John Smith, MD".</summary>
        [Fact]
        public void WithMdCredential_FormatsCorrectly()
        {
            var result = NameFormatting.ToTitleCaseFirstLastWithSuffixes("SMITH, JOHN, MD");
            Assert.Equal("John Smith, MD", result);
        }

        /// <summary>"SMITH, JOHN, JR, MD" is converted to "John Smith, Jr., MD".</summary>
        [Fact]
        public void WithMultipleCredentials_FormatsAll()
        {
            var result = NameFormatting.ToTitleCaseFirstLastWithSuffixes("SMITH, JOHN, JR, MD");
            Assert.Equal("John Smith, Jr., MD", result);
        }

        /// <summary>Credentials embedded in the first-name segment are extracted and appended.</summary>
        [Fact]
        public void EmbeddedCredentialsInFirst_Extracted()
        {
            var result = NameFormatting.ToTitleCaseFirstLastWithSuffixes("SMITH, JOHN MD");
            Assert.Equal("John Smith, MD", result);
        }

        /// <summary>A name without a comma is title-cased as a whole string.</summary>
        [Fact]
        public void NoComma_TitleCasesWholeString()
        {
            var result = NameFormatting.ToTitleCaseFirstLastWithSuffixes("JOHN SMITH");
            Assert.Equal("John Smith", result);
        }

        /// <summary>A hyphenated last name capitalises each part after the hyphen.</summary>
        [Fact]
        public void HyphenatedLastName_CapitalizedAfterHyphen()
        {
            var result = NameFormatting.ToTitleCaseFirstLastWithSuffixes("DOE-SMITH, JANE");
            Assert.Equal("Jane Doe-Smith", result);
        }

        /// <summary>An apostrophe name capitalises the character following the apostrophe.</summary>
        [Fact]
        public void ApostropheName_CapitalizedAfterApostrophe()
        {
            var result = NameFormatting.ToTitleCaseFirstLastWithSuffixes("O'BRIEN, KEVIN");
            Assert.Equal("Kevin O'Brien", result);
        }

        /// <summary>Roman numeral suffixes are uppercased (e.g., II).</summary>
        [Fact]
        public void RomanNumeralSuffix_Uppercased()
        {
            var result = NameFormatting.ToTitleCaseFirstLastWithSuffixes("SMITH, JOHN, II");
            Assert.Equal("John Smith, II", result);
        }

        /// <summary>Duplicate credentials are deduplicated in the output.</summary>
        [Fact]
        public void DuplicateCredentials_Deduplicated()
        {
            var result = NameFormatting.ToTitleCaseFirstLastWithSuffixes("SMITH, JOHN, MD, MD");
            Assert.Equal("John Smith, MD", result);
        }

        /// <summary>Extra whitespace around and within name segments is normalized.</summary>
        [Fact]
        public void ExtraWhitespace_Normalized()
        {
            var result = NameFormatting.ToTitleCaseFirstLastWithSuffixes("SMITH,  JOHN  ");
            Assert.Equal("John Smith", result);
        }
    }
}
