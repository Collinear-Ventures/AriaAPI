// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using System.Text.RegularExpressions;
using AriaAPI.API.IdentityResolvers;
using Xunit;

namespace AriaAPI.Tests.IdentityResolvers
{
    /// <summary>
    /// Tests for <see cref="PhiMask.Mask"/>.
    /// PhiMask is internal and accessible via InternalsVisibleTo.
    /// </summary>
    public sealed class PhiMaskTests
    {
        /// <summary>The result is always an 8-character lowercase hex string.</summary>
        [Fact]
        public void Mask_AnyInput_ReturnsEightCharHexString()
        {
            var result = PhiMask.Mask("patient-name");

            Assert.Equal(8, result.Length);
            Assert.Matches(new Regex("^[0-9a-f]{8}$"), result);
        }

        /// <summary>Null input is treated as empty and does not throw.</summary>
        [Fact]
        public void Mask_NullInput_DoesNotThrow()
        {
            var ex = Record.Exception(() => PhiMask.Mask(null));
            Assert.Null(ex);
        }

        /// <summary>The same input always produces the same hash (deterministic).</summary>
        [Fact]
        public void Mask_SameValueTwice_ReturnsSameHash()
        {
            var first = PhiMask.Mask("John Smith");
            var second = PhiMask.Mask("John Smith");

            Assert.Equal(first, second);
        }

        /// <summary>Two different values produce different hashes.</summary>
        [Fact]
        public void Mask_DifferentValues_ReturnDifferentHashes()
        {
            var hash1 = PhiMask.Mask("Alice");
            var hash2 = PhiMask.Mask("Bob");

            Assert.NotEqual(hash1, hash2);
        }
    }
}
