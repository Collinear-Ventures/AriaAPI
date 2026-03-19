// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using System;
using Xunit;
using static AriaAPI.API.Create.CreateHelpers;

namespace AriaAPI.Tests.Create
{
    /// <summary>
    /// Tests for <see cref="ContentTypeHelper.MapFromExtension"/> and <see cref="HashHelper.Sha256Hex"/>.
    /// </summary>
    public sealed class CreateHelpersTests
    {
        // ── ContentTypeHelper ──────────────────────────────────────────────────

        [Fact]
        public void MapFromExtension_Docx_ReturnsDocxMimeType()
        {
            var result = ContentTypeHelper.MapFromExtension(".docx");
            Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document", result);
        }

        [Fact]
        public void MapFromExtension_Doc_ReturnsMsWord()
        {
            var result = ContentTypeHelper.MapFromExtension(".doc");
            Assert.Equal("application/msword", result);
        }

        [Fact]
        public void MapFromExtension_Pdf_ReturnsPdf()
        {
            var result = ContentTypeHelper.MapFromExtension(".pdf");
            Assert.Equal("application/pdf", result);
        }

        [Fact]
        public void MapFromExtension_UnknownExtension_ReturnsOctetStream()
        {
            var result = ContentTypeHelper.MapFromExtension(".xyz");
            Assert.Equal("application/octet-stream", result);
        }

        [Fact]
        public void MapFromExtension_Null_ReturnsOctetStream()
        {
            var result = ContentTypeHelper.MapFromExtension(null);
            Assert.Equal("application/octet-stream", result);
        }

        [Fact]
        public void MapFromExtension_EmptyString_ReturnsOctetStream()
        {
            var result = ContentTypeHelper.MapFromExtension(string.Empty);
            Assert.Equal("application/octet-stream", result);
        }

        [Fact]
        public void MapFromExtension_UppercasePdf_ReturnsPdf()
        {
            var result = ContentTypeHelper.MapFromExtension(".PDF");
            Assert.Equal("application/pdf", result);
        }

        // ── HashHelper ─────────────────────────────────────────────────────────

        [Fact]
        public void Sha256Hex_EmptyBytes_Returns64CharLowercaseHex()
        {
            var result = HashHelper.Sha256Hex(Array.Empty<byte>());
            Assert.Equal(64, result.Length);
            Assert.Equal(result, result.ToLowerInvariant());
        }

        [Fact]
        public void Sha256Hex_SameInput_ReturnsSameHash()
        {
            var input = new byte[] { 1, 2, 3, 4 };
            var hash1 = HashHelper.Sha256Hex(input);
            var hash2 = HashHelper.Sha256Hex(input);
            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void Sha256Hex_DifferentInputs_ReturnDifferentHashes()
        {
            var hash1 = HashHelper.Sha256Hex(new byte[] { 1 });
            var hash2 = HashHelper.Sha256Hex(new byte[] { 2 });
            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void Sha256Hex_EmptyBytes_ReturnsKnownHash()
        {
            // SHA-256 of empty byte array is well-known
            var expected = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
            var result = HashHelper.Sha256Hex(Array.Empty<byte>());
            Assert.Equal(expected, result);
        }
    }
}
