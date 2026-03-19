// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using System;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using AriaAPI.Resources.Includes;
using AriaAPI.Networking.Core;

namespace AriaAPI.Networking.Helpers
{
    /// <summary>
    /// Strongly-typed _include / _revinclude helpers for <see cref="Builder{TResource}"/>.
    /// Keeps your existing Builder class unchanged.
    /// </summary>
    public static class BuilderExtensions
    {
        /// <summary>
        /// Adds a FHIR _include by deriving the resource type from TResource and
        /// the include segment from the enum value.
        /// Example: <c>new Builder&lt;DocumentReference&gt;().Include(DocumentReferenceInclude.Author)</c>
        /// produces "DocumentReference:author".
        /// </summary>
        public static Builder<TResource> Include<TResource, TInclude>(
            this Builder<TResource> builder,
            TInclude include,
            string? targetType = null,
            IncludeModifier modifier = IncludeModifier.None,
            bool validateResourceMatch = true)
            where TResource : Resource
            where TInclude : struct, Enum
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));

            var segment = include.ToSegment(); // from FhirIncludeSegments
            if (string.IsNullOrWhiteSpace(segment))
                return builder; // Treat sentinel "None" as no-op.

            var resourceType = typeof(TResource).Name;               // e.g., "Patient"
            var enumResource = GetEnumResourceName<TInclude>();      // e.g., "Patient" from "PatientInclude"

            // Guard against mixing the wrong enum with the current resource
            if (validateResourceMatch && !resourceType.Equals(enumResource, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Include enum '{typeof(TInclude).Name}' belongs to resource '{enumResource}', " +
                    $"but this Builder targets '{resourceType}'. " +
                    $"Pass validateResourceMatch: false to override, or use the RevInclude overload for reverse includes.");
            }

            return builder.Include($"{resourceType}:{segment}", targetType, modifier);
        }

        /// <summary>
        /// Adds multiple includes for TResource in one call.
        /// </summary>
        public static Builder<TResource> Include<TResource, TInclude>(
            this Builder<TResource> builder,
            params TInclude[] includes)
            where TResource : Resource
            where TInclude : struct, Enum
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (includes is null || includes.Length == 0) return builder;

            foreach (var inc in includes)
            {
                builder.Include(inc); // calls the typed overload above
            }
            return builder;
        }

        /// <summary>
        /// Adds a FHIR _revinclude by inferring the *source* resource type from the include enum name,
        /// e.g., TaskInclude.BasedOn -> "Task:based-on".
        /// TResource is the *target* resource of the query (the bundle entries you want back).
        /// </summary>
        public static Builder<TResource> RevIncludeFrom<TResource, TSourceInclude>(
            this Builder<TResource> builder,
            TSourceInclude include,
            string? targetType = null,
            IncludeModifier modifier = IncludeModifier.None)
            where TResource : Resource
            where TSourceInclude : struct, Enum
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));

            var segment = include.ToSegment(); // from FhirIncludeSegments
            if (string.IsNullOrWhiteSpace(segment))
                return builder; // No-op for sentinel

            var sourceResource = GetEnumResourceName<TSourceInclude>(); // e.g., "Task"
            return builder.RevInclude($"{sourceResource}:{segment}", targetType, modifier);
        }

        /// <summary>
        /// Adds multiple reverse includes by inferring their source resource types from enum names.
        /// </summary>
        public static Builder<TResource> RevIncludeFrom<TResource, TSourceInclude>(
            this Builder<TResource> builder,
            params TSourceInclude[] includes)
            where TResource : Resource
            where TSourceInclude : struct, Enum
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (includes is null || includes.Length == 0) return builder;

            foreach (var inc in includes)
            {
                builder.RevIncludeFrom(inc);
            }
            return builder;
        }

        private static string GetEnumResourceName<TInclude>() where TInclude : struct, Enum
        {
            var name = typeof(TInclude).Name; // e.g., "DocumentReferenceInclude"
            return name.EndsWith("Include", StringComparison.Ordinal)
                ? name.Substring(0, name.Length - "Include".Length)
                : name;
        }
    }
}