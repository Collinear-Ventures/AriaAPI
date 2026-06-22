// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AriaAPI.API.Create;
using AriaAPI.API.SearchHelpers;
using AriaAPI.Core;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using CodeableConcept = Hl7.Fhir.Model.CodeableConcept;

namespace AriaAPI.API.DocumentReferenceCreate
{
    /// <summary>
    /// Entry point to create DocumentReference resources with an embedded Attachment.
    /// </summary>
    public static class DocumentReferenceCreate
    {
        /// <summary>
        /// Parameters required to create a DocumentReference with an embedded Attachment.
        /// </summary>
        public sealed class DocumentReferenceCreateParams
        {
            /// <summary>FHIR reference to patient (e.g., "Patient/123").</summary>
            public string? PatientReference { get; init; }

            /// <summary>Optional display for patient.</summary>
            public string? PatientDisplay { get; init; }

            /// <summary>FHIR reference to author.</summary>
            public string? AuthorReference { get; init; }

            /// <summary>Optional display for author.</summary>
            public string? AuthorDisplay { get; init; }

            /// <summary>FHIR reference to authenticator (preferred Practitioner).</summary>
            public string? AuthenticatorReference { get; init; }

            /// <summary>Optional display for authenticator.</summary>
            public string? AuthenticatorDisplay { get; init; }

            /// <summary>FHIR reference to custodian organization.</summary>
            public string? CustodianReference { get; init; }

            /// <summary>Optional display for custodian.</summary>
            public string? CustodianDisplay { get; init; }

            /// <summary>Organization used for document type resolution.</summary>
            public string? DocumentTypeResolverOrganizationReference { get; init; }

            /// <summary>DocumentReference.status.</summary>
            public string Status { get; init; } = "current";

            /// <summary>DocumentReference.docStatus.</summary>
            public string? DocStatus { get; init; } = "final";

            /// <summary>Document type enum.</summary>
            public SearchTypes.DocumentType? Type { get; init; }

            /// <summary>Document creation date.</summary>
            public DateTime? Date { get; init; }

            /// <summary>Document description.</summary>
            public string? Description { get; init; }

            /// <summary>Optional identifiers.</summary>
            public List<string>? Identifiers { get; init; }

            /// <summary>Attachment title.</summary>
            public string? Title { get; init; }

            /// <summary>File path to embed.</summary>
            public string SourceFilePath { get; init; } = string.Empty;

            /// <summary>Attachment creation timestamp.</summary>
            public DateTime? Creation { get; init; }

            /// <summary>Document categories.</summary>
            public List<CodeableConcept> Category { get; init; } =
                new()
                {
                    new CodeableConcept
                    {
                        Coding =
                        {
                            new Coding(
                                "http://varian.com/fhir/CodeSystem/DocumentReference/documentreference-class",
                                "Patient Document",
                                "Patient Document")
                        }
                    }
                };

            /// <summary>Supervisor reference (Varian extension).</summary>
            public string? SupervisorReference { get; init; }

            /// <summary>Supervisor display.</summary>
            public string? SupervisorDisplay { get; init; }

            /// <summary>Authenticated timestamp (Varian extension).</summary>
            public DateTime? AuthenticatedDate { get; init; }

            /// <summary>Template name (Varian extension).</summary>
            public string? TemplateName { get; init; }

            /// <summary>Institution reference (Varian extension).</summary>
            public string? InstitutionReference { get; init; }

            /// <summary>Institution display.</summary>
            public string? InstitutionDisplay { get; init; }

            /// <summary>Document storage location (Varian extension).</summary>
            public string? DocumentLocation { get; init; }
        }

        /// <summary>Default max file size = 10 MB.</summary>
        public const long DefaultMaxFileSizeBytes = 10485760L;

        /// <summary>
        /// Creates a DocumentReference from a file.
        /// </summary>
        public static async Task<DocumentReference> CreateFromFileAsync(
            ClientConfigurator configurator,
            DocumentReferenceCreateParams p,
            ILogger logger,
            long maxFileSizeBytes = DefaultMaxFileSizeBytes,
            CancellationToken ct = default)
        {
            if (configurator == null)
                throw new ArgumentNullException(nameof(configurator));
            if (p == null)
                throw new ArgumentNullException(nameof(p));
            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            if (string.IsNullOrWhiteSpace(p.SourceFilePath))
                throw new FileNotFoundException("SourceFilePath must be specified.", p.SourceFilePath);

            if (!p.Type.HasValue)
                throw new ArgumentException("Document Type is required.", nameof(p));

            string path = Path.GetFullPath(p.SourceFilePath);

            if (!File.Exists(path))
                throw new FileNotFoundException($"File not found: {path}", path);

            FileInfo fi = new(path);

            if (fi.Length > maxFileSizeBytes)
                throw new InvalidOperationException($"File too large: {fi.Length}");

            ct.ThrowIfCancellationRequested();

            string resolverRef = GetResolver(p);
            string resolverId = ExtractId(resolverRef);

            var ccType = (await DocumentTypeConceptService
                    .CreateAsync(configurator, resolverId)
                    .ConfigureAwait(false))
                .Resolve(p.Type.Value);

            byte[] bytes = await File.ReadAllBytesAsync(path, ct);
            string contentType = CreateHelpers.ContentTypeHelper.MapFromExtension(Path.GetExtension(path));

            var attachment = new Attachment
            {
                ContentType = contentType,
                Title = p.Title ?? Path.GetFileName(path),
                Data = bytes,
                Size = bytes.Length,
                CreationElement = p.Creation.HasValue ? new FhirDateTime(p.Creation.Value) : null
            };

            var doc = new DocumentReference
            {
                Status = ParseStatus(p.Status),
                DocStatus = ParseDocStatus(p.DocStatus),
                DateElement = p.Date.HasValue ? new Instant(p.Date.Value) : null,
                Type = ccType.ToFhirCodeableConcept(),
                Description = p.Description,
                Content = new List<DocumentReference.ContentComponent>
                {
                    new() { Attachment = attachment }
                },
                Category = p.Category
            };

            AddRef(() => doc.Subject = CreateRef(p.PatientReference ?? throw new ArgumentNullException(nameof(p.PatientReference)), p.PatientDisplay), p.PatientReference);
            AddAuthor(doc, p);
            AddRef(() => doc.Authenticator = CreateRef(p.AuthenticatorReference ?? throw new ArgumentNullException(nameof(p.AuthenticatorReference)), p.AuthenticatorDisplay), p.AuthenticatorReference);
            AddRef(() => doc.Custodian = CreateRef(p.CustodianReference ?? throw new ArgumentNullException(nameof(p.CustodianReference)), p.CustodianDisplay), p.CustodianReference);

            AddIdentifiers(doc, p, bytes);
            AddExtensions(doc, p);

            var created = await configurator
                .ForResource<DocumentReference>(ct)
                .CreateAsync(doc);

            logger.LogInformation("Created DocumentReference {Id}", created?.Id);

            return created ?? throw new InvalidOperationException("Failed to create DocumentReference");
        }

        /// <summary>Adds author or fallback.</summary>
        private static void AddAuthor(DocumentReference doc, DocumentReferenceCreateParams p)
        {
            if (!string.IsNullOrWhiteSpace(p.AuthorReference))
            {
                doc.Author = new List<ResourceReference>
                {
                    CreateRef(p.AuthorReference, p.AuthorDisplay)
                };
            }
            else if (!string.IsNullOrWhiteSpace(p.AuthenticatorReference))
            {
                doc.Author = new List<ResourceReference>
                {
                    CreateRef(p.AuthenticatorReference, p.AuthenticatorDisplay)
                };
            }
        }

        /// <summary>Add identifiers + SHA256.</summary>
        private static void AddIdentifiers(DocumentReference doc, DocumentReferenceCreateParams p, byte[] bytes)
        {
            doc.Identifier ??= new List<Identifier>();

            if (p.Identifiers != null)
            {
                foreach (var id in p.Identifiers)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                        doc.Identifier.Add(new Identifier("urn:aria:doc", id));
                }
            }

            doc.Identifier.Add(new Identifier("urn:hash:sha256", CreateHelpers.HashHelper.Sha256Hex(bytes)));
        }

        /// <summary>Add Varian extensions.</summary>
        private static void AddExtensions(DocumentReference doc, DocumentReferenceCreateParams p)
        {
            var ext = new List<Extension>();

            if (!string.IsNullOrWhiteSpace(p.SupervisorReference))
                ext.Add(new Extension("http://varian.com/fhir/v1/StructureDefinition/documentreference-supervisor",
                    CreateRef(p.SupervisorReference, p.SupervisorDisplay)));

            if (p.AuthenticatedDate.HasValue)
                ext.Add(new Extension("http://varian.com/fhir/v1/StructureDefinition/documentreference-authenticated",
                    new FhirDateTime(p.AuthenticatedDate.Value)));

            if (!string.IsNullOrWhiteSpace(p.TemplateName))
                ext.Add(new Extension("http://varian.com/fhir/v1/StructureDefinition/documentreference-templateName",
                    new FhirString(p.TemplateName)));

            if (!string.IsNullOrWhiteSpace(p.InstitutionReference))
                ext.Add(new Extension("http://varian.com/fhir/v1/StructureDefinition/login-institution",
                    CreateRef(p.InstitutionReference, p.InstitutionDisplay)));

            if (!string.IsNullOrWhiteSpace(p.DocumentLocation))
                ext.Add(new Extension("http://varian.com/fhir/v1/StructureDefinition/documentreference-documentLocation",
                    new FhirString(p.DocumentLocation)));

            if (ext.Count > 0)
                doc.Extension = ext;
        }

        private static ResourceReference CreateRef(string reference, string? display = null)
        {
            var r = new ResourceReference(reference);
            if (!string.IsNullOrWhiteSpace(display))
                r.Display = display;
            return r;
        }

        private static void AddRef(Action setter, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                setter();
        }

        private static string GetResolver(DocumentReferenceCreateParams p) =>
            p.DocumentTypeResolverOrganizationReference ??
            p.InstitutionReference ??
            p.CustodianReference ??
            p.AuthenticatorReference ??
            throw new ArgumentException("No resolver reference provided");

        private static string ExtractId(string reference)
        {
            var parts = reference.Split('/');
            if (parts.Length < 2)
                throw new ArgumentException("Invalid FHIR reference");
            return parts[1];
        }

        private static DocumentReferenceStatus ParseStatus(string? s) =>
            (s ?? "current").ToLower() switch
            {
                "entered-in-error" => DocumentReferenceStatus.EnteredInError,
                "superseded" => DocumentReferenceStatus.Superseded,
                _ => DocumentReferenceStatus.Current
            };

        private static CompositionStatus? ParseDocStatus(string? s) =>
            (s ?? "final").ToLower() switch
            {
                "preliminary" => CompositionStatus.Preliminary,
                "entered-in-error" => CompositionStatus.EnteredInError,
                "amended" => CompositionStatus.Amended,
                _ => CompositionStatus.Final
            };
    }
}
