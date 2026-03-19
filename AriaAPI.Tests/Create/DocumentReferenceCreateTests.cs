// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using AriaAPI.API.DocumentReferenceCreate;
using AriaAPI.Core;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace AriaAPI.Tests.Create
{
    /// <summary>
    /// Tests for validation guards in <see cref="DocumentReferenceCreate.CreateFromFileAsync"/>.
    /// All cases throw before any FHIR call is made.
    /// </summary>
    public sealed class DocumentReferenceCreateTests
    {
        private static readonly NullLogger<DocumentReferenceCreateTests> Logger = NullLogger<DocumentReferenceCreateTests>.Instance;

        /// <summary>
        /// Returns a non-null <see cref="ClientConfigurator"/> whose fields are uninitialized.
        /// Safe to use only in tests where the guard fires before the configurator is dereferenced.
        /// </summary>
        private static ClientConfigurator UninitializedConfigurator() =>
            (ClientConfigurator)RuntimeHelpers.GetUninitializedObject(typeof(ClientConfigurator));

        [Fact]
        public async Task CreateFromFileAsync_NullConfigurator_ThrowsArgumentNullException()
        {
            var p = new DocumentReferenceCreate.DocumentReferenceCreateParams
            {
                SourceFilePath = "some/path.pdf"
            };

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                DocumentReferenceCreate.CreateFromFileAsync(null!, p, Logger));

            Assert.Equal("configurator", ex.ParamName);
        }

        [Fact]
        public async Task CreateFromFileAsync_NullParams_ThrowsArgumentNullException()
        {
            var configurator = UninitializedConfigurator();

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                DocumentReferenceCreate.CreateFromFileAsync(configurator, null!, Logger));

            Assert.Equal("p", ex.ParamName);
        }

        [Fact]
        public async Task CreateFromFileAsync_EmptySourceFilePath_ThrowsFileNotFoundException()
        {
            var configurator = UninitializedConfigurator();
            var p = new DocumentReferenceCreate.DocumentReferenceCreateParams
            {
                SourceFilePath = string.Empty
            };

            await Assert.ThrowsAsync<FileNotFoundException>(() =>
                DocumentReferenceCreate.CreateFromFileAsync(configurator, p, Logger));
        }

        [Fact]
        public async Task CreateFromFileAsync_NonExistentFilePath_ThrowsFileNotFoundException()
        {
            var configurator = UninitializedConfigurator();
            var p = new DocumentReferenceCreate.DocumentReferenceCreateParams
            {
                SourceFilePath = "/this/path/does/not/exist/file.pdf"
            };

            await Assert.ThrowsAsync<FileNotFoundException>(() =>
                DocumentReferenceCreate.CreateFromFileAsync(configurator, p, Logger));
        }

        [Fact]
        public async Task CreateFromFileAsync_FileTooLarge_ThrowsInvalidOperationException()
        {
            var configurator = UninitializedConfigurator();
            var tmpFile = Path.GetTempFileName();
            try
            {
                // Write 100 bytes, set limit to 10 bytes
                await File.WriteAllBytesAsync(tmpFile, new byte[100]);

                var p = new DocumentReferenceCreate.DocumentReferenceCreateParams
                {
                    SourceFilePath = tmpFile
                };

                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    DocumentReferenceCreate.CreateFromFileAsync(configurator, p, Logger, maxFileSizeBytes: 10));
            }
            finally
            {
                File.Delete(tmpFile);
            }
        }

        [Fact]
        public async Task CreateFromFileAsync_MissingAuthenticatorReference_ThrowsArgumentException()
        {
            var configurator = UninitializedConfigurator();
            var tmpFile = Path.GetTempFileName();
            try
            {
                await File.WriteAllBytesAsync(tmpFile, new byte[10]);

                var p = new DocumentReferenceCreate.DocumentReferenceCreateParams
                {
                    SourceFilePath = tmpFile,
                    AuthenticatorReference = string.Empty
                };

                await Assert.ThrowsAsync<ArgumentException>(() =>
                    DocumentReferenceCreate.CreateFromFileAsync(configurator, p, Logger));
            }
            finally
            {
                File.Delete(tmpFile);
            }
        }

        [Fact]
        public async Task CreateFromFileAsync_MalformedAuthenticatorReference_ThrowsArgumentException()
        {
            var configurator = UninitializedConfigurator();
            var tmpFile = Path.GetTempFileName();
            try
            {
                await File.WriteAllBytesAsync(tmpFile, new byte[10]);

                var p = new DocumentReferenceCreate.DocumentReferenceCreateParams
                {
                    SourceFilePath = tmpFile,
                    AuthenticatorReference = "NoSlashHere"
                };

                await Assert.ThrowsAsync<ArgumentException>(() =>
                    DocumentReferenceCreate.CreateFromFileAsync(configurator, p, Logger));
            }
            finally
            {
                File.Delete(tmpFile);
            }
        }

        [Fact]
        public async Task CreateFromFileAsync_PathWithTraversal_ResolvesToFullPath()
        {
            // Arrange - a path with traversal sequences that won't exist
            var traversalPath = "/tmp/../tmp/../nonexistent/file.pdf";
            var expectedResolvedPath = Path.GetFullPath(traversalPath);

            var configurator = UninitializedConfigurator();
            var p = new DocumentReferenceCreate.DocumentReferenceCreateParams
            {
                SourceFilePath = traversalPath,
                AuthenticatorReference = "Organization/Test",
                Type = AriaAPI.API.SearchHelpers.SearchTypes.DocumentType.AdvanceDirective
            };

            // Act & Assert - should throw FileNotFoundException with the resolved path
            var ex = await Assert.ThrowsAsync<FileNotFoundException>(() =>
                DocumentReferenceCreate.CreateFromFileAsync(configurator, p, Logger));

            // The exception should reference the resolved path, not the traversal path
            Assert.Equal(expectedResolvedPath, ex.FileName);
        }
    }
}
