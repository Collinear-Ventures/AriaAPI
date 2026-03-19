// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using AriaAPI.Networking.Helpers;
using AriaAPI.Resources;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;

namespace AriaAPI.Networking.Core
{
    /// <summary>
    /// Fluent builder over FHIR SearchParams. Produces a SearchParams ready for use
    /// with <see cref="AriaFhirClient{TResource}"/>, relying on native FHIR search keys.
    /// </summary>
    public sealed class Builder<TResource> where TResource : Resource
    {
        private readonly SearchParams _sp = new();

        /// <summary>Builds the underlying SearchParams (immutable snapshot).</summary>
        public SearchParams Build() => _sp;

        /// <summary>Set preferred page size.</summary>
        public Builder<TResource> WithCount(int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "Count must be > 0.");
            _sp.Count = count;
            return this;
        }

        /// <summary>Set FHIR _summary mode.</summary>
        public Builder<TResource> WithSummary(SummaryType summary)
        {
            _sp.Summary = summary;
            return this;
        }

        /// <summary>Generic key/value addition. Values are not escaped; ensure proper values per FHIR spec.</summary>
        public Builder<TResource> With(string key, string value)
        {
            key = Ensure.NotNullOrWhiteSpace(key, nameof(key));
            value = Ensure.NotNullOrWhiteSpace(value, nameof(value));
            _sp.Add(key, value);
            return this;
        }

        /// <summary>Common: search by logical id via "_id".</summary>
        public Builder<TResource> ById(string id) => With("_id", Ensure.NotNullOrWhiteSpace(id, nameof(id)));

        /// <summary>Common: search by business identifier.</summary>
        public Builder<TResource> ByIdentifier(string identifier) => With("identifier", Ensure.NotNullOrWhiteSpace(identifier, nameof(identifier)));

        /// <summary>Patient-scoped: adds "patient={id}" and/or "subject=Patient/{id}" where applicable.
        /// Use the key that your server supports; default here is "patient".</summary>
        public Builder<TResource> ForPatient(string patientId, bool useSubject = false)
        {
            Ensure.NotNullOrWhiteSpace(patientId, nameof(patientId));
            if (useSubject)
                _sp.Add("subject", $"Patient/{patientId}");
            else
                _sp.Add("patient", patientId);
            return this;
        }

        /// <summary>Name-based lookup (when resources define it, e.g., Organization, Group).</summary>
        public Builder<TResource> ByName(string name) => With("name", Ensure.NotNullOrWhiteSpace(name, nameof(name)));

        /// <summary>Name-based lookup (when resources define it, e.g., Organization, Group).</summary>
        public Builder<TResource> ByType(string name) => With("type", Ensure.NotNullOrWhiteSpace(name, nameof(name)));

        /// <summary>Code/scope lookups (ValueSet, ActivityDefinition, etc.).</summary>
        public Builder<TResource> ByCode(string code) => With("code", Ensure.NotNullOrWhiteSpace(code, nameof(code)));

        /// <summary>Sort support; e.g., "-date" to sort descending.</summary>
        public Builder<TResource> Sort(string sortBy) => With("_sort", Ensure.NotNullOrWhiteSpace(sortBy, nameof(sortBy)));

        /// <summary>FHIR _include. Accepts raw "Resource:path" and optional target type.</summary>
        public Builder<TResource> Include(string include, string? targetType = null, IncludeModifier modifier = IncludeModifier.None)
        {
            include = Ensure.NotNullOrWhiteSpace(include, nameof(include));

            // Firely SDK expects the target type, if any, to be part of the string: "Resource:path:TargetType"
            var composed = string.IsNullOrWhiteSpace(targetType)
                ? include
                : $"{include}:{Ensure.NotNullOrWhiteSpace(targetType, nameof(targetType))}";

            _sp.Include.Add((composed, modifier));
            return this;
        }

        /// <summary>FHIR _revinclude. Accepts raw "Resource:path" and optional source type.</summary>
        public Builder<TResource> RevInclude(string revInclude, string? targetType = null, IncludeModifier modifier = IncludeModifier.None)
        {
            revInclude = Ensure.NotNullOrWhiteSpace(revInclude, nameof(revInclude));

            var composed = string.IsNullOrWhiteSpace(targetType)
                ? revInclude
                : $"{revInclude}:{Ensure.NotNullOrWhiteSpace(targetType, nameof(targetType))}";

            _sp.RevInclude.Add((composed, modifier));
            return this;
        }


        /// <summary>Clears all include/revinclude directives.</summary>
        public Builder<TResource> ClearIncludes()
        {
            _sp.Include.Clear();
            _sp.RevInclude.Clear();
            return this;
        }


    }


}