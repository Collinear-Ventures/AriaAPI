// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿// AriaFhirClient.cs
using AriaAPI.Core;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace AriaAPI.Networking.Core
{
    /// <summary>
    /// Single client type for all FHIR resources of type TResource.
    /// Uses native FHIR SearchParams for queries and returns fully-typed results.
    /// </summary>
    public sealed class AriaFhirClient<TResource> where TResource : Resource
    {
        private readonly ClientConfigurator _clientConfigurator;
        private readonly CancellationToken _ct;
        private FhirClient Fhir => _clientConfigurator.FhirClient;
        private readonly string _resourceTypeName;

        /// <summary>
        /// Initializes a new instance of the <see cref="AriaFhirClient{TResource}"/> class.
        /// </summary>
        /// <param name="clientConfigurator">The client configurator providing the authenticated FHIR client.</param>
        /// <param name="ct">Cancellation token forwarded to all FHIR operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="clientConfigurator"/> is <see langword="null"/>.</exception>
        public AriaFhirClient(ClientConfigurator clientConfigurator, CancellationToken ct = default)
        {
            _clientConfigurator = clientConfigurator ?? throw new System.ArgumentNullException(nameof(clientConfigurator));
            _ct = ct;
            _resourceTypeName = typeof(TResource).Name; // e.g., "Patient", "Observation"

        }

        #region CRUD

        /// <summary>
        /// Creates a new FHIR resource on the server and returns the server-assigned resource.
        /// </summary>
        /// <param name="resource">The resource to create. Must not be <see langword="null"/>.</param>
        /// <returns>The created resource as returned by the server, or <see langword="null"/> if the server returns no content.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="resource"/> is <see langword="null"/>.</exception>
        public Task<TResource?> CreateAsync(TResource resource)
        {
            if (resource is null) throw new System.ArgumentNullException(nameof(resource));
            return Fhir.CreateAsync(resource, _ct);
        }

        /// <summary>
        /// Reads a FHIR resource by its logical ID, optionally targeting a specific version.
        /// </summary>
        /// <param name="id">The logical resource ID. Must not be null or whitespace.</param>
        /// <param name="versionId">
        /// Optional version ID for history reads (<c>_history/{versionId}</c>).
        /// When <see langword="null"/>, the latest version is returned.
        /// </param>
        /// <returns>The resource, or <see langword="null"/> if not found.</returns>
        public Task<TResource?> ReadAsync(string id, string? versionId = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new System.ArgumentException("ID cannot be null or empty.", nameof(id));
            var relative = versionId is null
                ? $"{_resourceTypeName}/{id}"
                : $"{_resourceTypeName}/{id}/_history/{versionId}";
            return Fhir.ReadAsync<TResource>(relative, ct: _ct);
        }

        /// <summary>
        /// Updates an existing FHIR resource on the server.
        /// </summary>
        /// <param name="resource">The resource to update. Must not be <see langword="null"/>.</param>
        /// <param name="versionAware">
        /// When <see langword="true"/> (default), sends an <c>If-Match</c> ETag header to enforce
        /// optimistic concurrency. Set to <see langword="false"/> to perform an unconditional update.
        /// </param>
        /// <returns>The updated resource as returned by the server, or <see langword="null"/> if no content.</returns>
        public Task<TResource?> UpdateAsync(TResource resource, bool versionAware = true)
        {
            if (resource is null) throw new System.ArgumentNullException(nameof(resource));
            return Fhir.UpdateAsync(resource, versionAware, ct: _ct);
        }

        /// <summary>
        /// Deletes a FHIR resource by its logical ID.
        /// </summary>
        /// <param name="id">The logical resource ID. Must not be null or whitespace.</param>
        /// <exception cref="System.ArgumentException">Thrown when <paramref name="id"/> is null or whitespace.</exception>
        public Task DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new System.ArgumentException("ID cannot be null or empty.", nameof(id));
            return Fhir.DeleteAsync($"{_resourceTypeName}/{id}", _ct);
        }

        /// <summary>
        /// Conditional delete using server-side search evaluation (e.g., "identifier=xyz").
        /// </summary>
        public Task DeleteConditionalAsync(string searchCriteria)
        {
            if (string.IsNullOrWhiteSpace(searchCriteria)) throw new System.ArgumentException("Search criteria cannot be null or empty.", nameof(searchCriteria));
            return Fhir.DeleteAsync($"{_resourceTypeName}?{searchCriteria}", _ct);
        }

        /// <summary>
        /// Conditionally updates an existing FHIR resource using server-side search evaluation.
        /// Issues a conditional PUT — creates the resource if no match is found, updates it if exactly one match is found.
        /// </summary>
        /// <param name="resource">The resource to create or update. Must not be <see langword="null"/>.</param>
        /// <param name="condition">
        /// Search parameters used to locate the existing resource (e.g., <c>identifier=system|value</c>).
        /// Must not be <see langword="null"/>.
        /// </param>
        /// <param name="versionAware">
        /// When <see langword="true"/>, sends an <c>If-Match</c> ETag header for optimistic concurrency.
        /// Defaults to <see langword="false"/> for upsert semantics.
        /// </param>
        /// <returns>The created or updated resource as returned by the server.</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="resource"/> or <paramref name="condition"/> is <see langword="null"/>.</exception>
        public Task<TResource?> ConditionalUpdateAsync(TResource resource, SearchParams condition, bool versionAware = false)
        {
            if (resource is null) throw new ArgumentNullException(nameof(resource));
            if (condition is null) throw new ArgumentNullException(nameof(condition));
            return Fhir.ConditionalUpdateAsync(resource, condition, versionAware, _ct);
        }

        #endregion

        #region Search

        /// <summary>
        /// Executes a search and returns the raw Bundle page. Use ContinueAsync to follow links.
        /// </summary>
        public Task<Bundle?> SearchBundleAsync(SearchParams sp)
        {
            if (sp is null) throw new System.ArgumentNullException(nameof(sp));
            return Fhir.SearchAsync(sp, _resourceTypeName, _ct);
        }

        /// <summary>
        /// Continues paging on a search result Bundle. Returns null when there are no more pages.
        /// </summary>
        public Task<Bundle?> ContinueAsync(Bundle bundle)
        {
            if (bundle is null) throw new ArgumentNullException(nameof(bundle));
            return Fhir.ContinueAsync(bundle, ct: _ct);
        }

        /// <summary>
        /// Convenience: executes a search and yields all TResource across all pages.
        /// </summary>
        public async IAsyncEnumerable<TResource> SearchAllAsync(SearchParams sp, int? pageSize = null)
        {
            if (sp is null) throw new ArgumentNullException(nameof(sp));
            if (pageSize.HasValue) sp.Count = pageSize.Value;

            var page = await SearchBundleAsync(sp).ConfigureAwait(false);
            while (page is not null)
            {
                foreach (var res in ExtractResources(page))
                    yield return res;

                page = await ContinueAsync(page).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Convenience: executes a search and returns the first page's resources as a list.
        /// </summary>
        public async Task<IReadOnlyList<TResource>> SearchFirstPageAsync(SearchParams sp, int? pageSize = null)
        {
            if (sp is null) throw new ArgumentNullException(nameof(sp));
            if (pageSize.HasValue) sp.Count = pageSize.Value;

            var page = await SearchBundleAsync(sp).ConfigureAwait(false);
            return ExtractResources(page ?? throw new NullReferenceException("Bundle cannot be null.")).ToList();
        }

        private static IEnumerable<TResource> ExtractResources(Bundle bundle)
        {
            if (bundle?.Entry is null) yield break;
            foreach (var e in bundle.Entry)
            {
                if (e.Resource is TResource typed)
                    yield return typed;
            }
        }

        #endregion

        #region Read by reference

        /// <summary>
        /// Reads a FHIR resource using a full relative reference URL (e.g., <c>Patient/123</c>).
        /// </summary>
        /// <param name="reference">The relative reference string. Must not be null or whitespace.</param>
        /// <returns>The resource at the given reference, or <see langword="null"/> if not found.</returns>
        public Task<TResource?> ReadByUrlAsync(string reference)
        {
            if (string.IsNullOrWhiteSpace(reference))
                throw new System.ArgumentException("Reference cannot be null or empty.", nameof(reference));
            return Fhir.ReadAsync<TResource>(reference, ct: _ct);
        }

        #endregion
    }
}