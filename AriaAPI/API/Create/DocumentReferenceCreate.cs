// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using AriaAPI.API.IdentityResolvers;
using AriaAPI.Core;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using static AriaAPI.API.Create.CreateHelpers;
using static AriaAPI.API.SearchHelpers.SearchTypes;
using CodeableConcept = Hl7.Fhir.Model.CodeableConcept;

namespace AriaAPI.API.DocumentReferenceCreate
{
    /// <summary>
    /// Entry point to create DocumentReference resources with an embedded Attachment.
    /// </summary>
    public static class DocumentReferenceCreate
    {
        /// <summary>
        /// Default maximum file size in bytes (10 MB).
        /// </summary>
        public const long DefaultMaxFileSizeBytes = 10L * 1024 * 1024;

        /// <summary>
        /// Parameters required to create a DocumentReference with an embedded Attachment.
        /// </summary>
        public sealed class DocumentReferenceCreateParams
        {
            /// <summary>FHIR reference to the patient, e.g., "Patient/123". If not set, the create will proceed without subject.</summary>
            public string? PatientReference { get; init; }

            /// <summary>FHIR reference to the author, e.g., "Practitioner/456". Optional.</summary>
            public string? AuthorReference { get; init; }

            /// <summary>FHIR reference to the authenticator, e.g., "Organization/RadOnc-1". Optional.</summary>
            public string? AuthenticatorReference { get; init; }

            /// <summary>DocumentReference.status: "current" | "entered-in-error" | "superseded". Defaults to "current".</summary>
            public string Status { get; init; } = "current";

            /// <summary>docStatus: "preliminary" | "final" | "entered-in-error" | "amended".</summary>
            public string? DocStatus { get; init; } = "final";

            /// <summary>Document type (domain enum). Will be mapped to a CodeableConcept.</summary>
            public DocumentType? Type { get; init; }

            /// <summary>Date/time the document was created (DocumentReference.date).</summary>
            public DateTime? Date { get; init; }

            /// <summary>Optional identifiers attached to DocumentReference.identifier.</summary>
            public List<string>? Identifiers { get; init; }

            /// <summary>Attachment title (usually file name).</summary>
            public string? Title { get; init; }

            /// <summary>Absolute file path to the artifact to embed as Attachment.data.</summary>
            public string SourceFilePath { get; init; } = default!;

            /// <summary>Creation timestamp for the attachment.</summary>
            public DateTime? Creation { get; init; }
            /// <summary>Category classification for the document reference. Defaults to "Patient Document".</summary>
            public List<CodeableConcept> Category { get; init; } = new List<CodeableConcept>() 
                                                                        { new CodeableConcept()
                                                                            { Coding = {
                                                                              new Coding("http://varian.com/fhir/CodeSystem/DocumentReference/documentreference-class",
                                                                              "Patient Document", 
                                                                              "Patient Document") } } };
        }   

        /// <summary>
        /// Reads the file at <see cref="DocumentReferenceCreateParams.SourceFilePath"/>,
        /// builds a DocumentReference, and creates it on the FHIR server.
        /// </summary>
        /// <param name="configurator">FHIR client configurator.</param>
        /// <param name="p">Parameters describing the document to create.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="maxFileSizeBytes">
        /// Maximum allowed file size in bytes. Files exceeding this limit cause an
        /// <see cref="InvalidOperationException"/>. Defaults to <see cref="DefaultMaxFileSizeBytes"/> (10 MB).
        /// </param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The created <see cref="DocumentReference"/> resource.</returns>
        /// <exception cref="FileNotFoundException">Thrown when <paramref name="p"/>.SourceFilePath does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the file exceeds <paramref name="maxFileSizeBytes"/>.</exception>
        public static async Task<DocumentReference> CreateFromFileAsync(
            ClientConfigurator configurator,
            DocumentReferenceCreateParams p,
            ILogger logger,
            long maxFileSizeBytes = DefaultMaxFileSizeBytes,
            CancellationToken ct = default)
        {
            if (configurator is null) throw new ArgumentNullException(nameof(configurator));
            if (p is null) throw new ArgumentNullException(nameof(p));

            if (string.IsNullOrWhiteSpace(p.SourceFilePath))
                throw new FileNotFoundException("SourceFilePath must be specified.", p.SourceFilePath);

            var resolvedPath = Path.GetFullPath(p.SourceFilePath);

            if (!File.Exists(resolvedPath))
                throw new FileNotFoundException($"Source file not found at path: {resolvedPath}", resolvedPath);

            var fileInfo = new FileInfo(resolvedPath);
            if (fileInfo.Length > maxFileSizeBytes)
                throw new InvalidOperationException(
                    $"File size ({fileInfo.Length:N0} bytes) exceeds the maximum allowed size ({maxFileSizeBytes:N0} bytes): {resolvedPath}");

            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(p.AuthenticatorReference))
                throw new ArgumentException(
                    "AuthenticatorReference is required to resolve document types (e.g., \"Organization/JamesRO\").",
                    nameof(p));

            var refParts = p.AuthenticatorReference.Split('/');
            if (refParts.Length < 2 || string.IsNullOrWhiteSpace(refParts[1]))
                throw new ArgumentException(
                    $"AuthenticatorReference must be in 'ResourceType/Id' format (e.g., \"Organization/JamesRO\"), got: \"{p.AuthenticatorReference}\".",
                    nameof(p));

            var service = await DocumentTypeConceptService.CreateAsync(
                    configurator,
                    publisher: refParts[1],
                    listReturnLimit: 250
                ).ConfigureAwait(false);

            // Resolve from your enum
            var ccType = service.Resolve(p.Type!.Value);

            // 1) Package file as Attachment (base64)
            var bytes = await File.ReadAllBytesAsync(resolvedPath, ct);
            var contentType = ContentTypeHelper.MapFromExtension(Path.GetExtension(resolvedPath));
            var title = string.IsNullOrWhiteSpace(p.Title) ? Path.GetFileName(resolvedPath) : p.Title;

            var attachment = new Attachment
            {
                ContentType = contentType,
                Title = title,
                Data = bytes,                          // The SDK will base64 encode for wire format
                Size = bytes.Length,
                CreationElement = p.Creation.HasValue ? new FhirDateTime(p.Creation.Value) : null
            };

            // 2) Build DocumentReference skeleton
            var docRef = new DocumentReference
            {
                Status = ParseDocRefStatus(p.Status),
                DocStatus = ParseDocStatus(p.DocStatus),
                DateElement = p.Date.HasValue ? new Instant(p.Date.Value) : null,
                Type = ccType.ToFhirCodeableConcept(),
                Content = new List<DocumentReference.ContentComponent>
                {
                    new DocumentReference.ContentComponent { Attachment = attachment }
                },
                Category = p.Category,
            };

            // 3) Subject (Patient), Author, Authenticator
            if (!string.IsNullOrWhiteSpace(p.PatientReference))
                docRef.Subject = new ResourceReference(p.PatientReference);
            if (!string.IsNullOrWhiteSpace(p.AuthorReference))
                docRef.Author = new List<ResourceReference> { new ResourceReference(p.AuthorReference) };
            if (!string.IsNullOrWhiteSpace(p.AuthenticatorReference))
                docRef.Authenticator = new ResourceReference(p.AuthenticatorReference);

            // 4) Identifiers (optional)
            if (p.Identifiers is { Count: > 0 })
            {
                docRef.Identifier = new List<Identifier>();
                foreach (var id in p.Identifiers!)
                {
                    docRef.Identifier.Add(new Identifier(system: "urn:aria:doc", value: id));
                }
            }

            // 5) Add a SHA256 hash of the content as an identifier for traceability (optional but handy)
            docRef.Identifier ??= new List<Identifier>();
            docRef.Identifier.Add(new Identifier(system: "urn:hash:sha256", value: HashHelper.Sha256Hex(bytes)));

            // 6) Create via resource client
            var docClient = configurator.ForResource<DocumentReference>(ct);
            var created = await docClient.CreateAsync(docRef).ConfigureAwait(false);

            logger.LogInformation("DocumentReference created with id: {Id}", PhiMask.Mask(created?.Id ?? ""));
            return created!;
        }

        private static DocumentReferenceStatus ParseDocRefStatus(string? s)
        {
            return (s ?? "current").ToLowerInvariant() switch
            {
                "current" => DocumentReferenceStatus.Current,
                "entered-in-error" => DocumentReferenceStatus.EnteredInError,
                "superseded" => DocumentReferenceStatus.Superseded,
                _ => DocumentReferenceStatus.Current
            };
        }

        private static CompositionStatus? ParseDocStatus(string? s)
        {
            return (s ?? "final").ToLowerInvariant() switch
            {
                "preliminary" => CompositionStatus.Preliminary,
                "final" => CompositionStatus.Final,
                "entered-in-error" => CompositionStatus.EnteredInError,
                "amended" => CompositionStatus.Amended,
                _ => CompositionStatus.Final
            };
        }
    }


}