// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using AriaAPI.Resources.Includes;
using Hl7.Fhir.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AriaAPI.Networking.Core
{

    /// <summary>
    /// Extensions over Builder&lt;TResource&gt; to add FHIR _include / _revinclude using enum values.
    /// </summary>
    public static class BuilderExtensions
    {

        /// <summary>
        /// Adds a single _include using an enum. The resource portion is derived from the enum type.
        /// Example: builder.Include(PatientInclude.ManagingOrganization) -> "Patient:managing-organization"
        /// </summary>
        public static Builder<TResource> Include<TResource, TInclude>(
            this Builder<TResource> builder,
            TInclude include,
            string? targetType = null,
            IncludeModifier modifier = IncludeModifier.None)
            where TResource : Hl7.Fhir.Model.Resource
            where TInclude : struct, Enum
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));

            var resourceType = FhirIncludeResources.GetResourceNameFor<TInclude>();
            var segment = FhirIncludeSegments.ToSegment(include);

            // Compose "Resource:segment[:TargetType]" the way your Builder.Include expects it
            var composed = string.IsNullOrWhiteSpace(targetType)
                ? $"{resourceType}:{segment}"
                : $"{resourceType}:{segment}:{targetType}";

            return builder.Include(composed, targetType: null, modifier); // targetType already composed
        }

        /// <summary>
        /// Adds multiple _include values using enum inputs. All are derived from the enum type's resource.
        /// </summary>
        public static Builder<TResource> Include<TResource, TInclude>(
            this Builder<TResource> builder,
            IEnumerable<TInclude> includes,
            string? targetType = null,
            IncludeModifier modifier = IncludeModifier.None)
            where TResource : Hl7.Fhir.Model.Resource
            where TInclude : struct, Enum
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (includes is null) return builder;

            foreach (var inc in includes)
                builder.Include(inc, targetType, modifier);

            return builder;
        }

        /// <summary>
        /// Adds multiple _include values using enum params.
        /// </summary>
        public static Builder<TResource> Include<TResource, TInclude>(
            this Builder<TResource> builder,
            string? targetType = null,
            IncludeModifier modifier = IncludeModifier.None,
            params TInclude[] includes)
            where TResource : Hl7.Fhir.Model.Resource
            where TInclude : struct, Enum
        {
            if (includes is null || includes.Length == 0) return builder;
            return builder.Include((IEnumerable<TInclude>)includes, targetType, modifier);
        }

        /// <summary>
        /// Adds a single _revinclude using an enum. The *source* resource portion is derived from the enum type.
        /// Example: builder.RevInclude(DocumentReferenceInclude.Author) -> "DocumentReference:author"
        /// </summary>
        public static Builder<TResource> RevInclude<TResource, TInclude>(
            this Builder<TResource> builder,
            TInclude revInclude,
            string? targetType = null,
            IncludeModifier modifier = IncludeModifier.None)
            where TResource : Hl7.Fhir.Model.Resource
            where TInclude : struct, Enum
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));

            var sourceResourceType = FhirIncludeResources.GetResourceNameFor<TInclude>();
            var segment = FhirIncludeSegments.ToSegment(revInclude);

            var composed = string.IsNullOrWhiteSpace(targetType)
                ? $"{sourceResourceType}:{segment}"
                : $"{sourceResourceType}:{segment}:{targetType}";

            return builder.RevInclude(composed, targetType: null, modifier); // already composed
        }

        /// <summary>
        /// Adds multiple _revinclude values using enum inputs.
        /// </summary>
        public static Builder<TResource> RevInclude<TResource, TInclude>(
            this Builder<TResource> builder,
            IEnumerable<TInclude> revIncludes,
            string? targetType = null,
            IncludeModifier modifier = IncludeModifier.None)
            where TResource : Hl7.Fhir.Model.Resource
            where TInclude : struct, Enum
        {
            if (builder is null) throw new ArgumentNullException(nameof(builder));
            if (revIncludes is null) return builder;

            foreach (var inc in revIncludes)
                builder.RevInclude(inc, targetType, modifier);

            return builder;
        }

        /// <summary>
        /// Adds multiple _revinclude values using enum params.
        /// </summary>
        public static Builder<TResource> RevInclude<TResource, TInclude>(
            this Builder<TResource> builder,
            string? targetType = null,
            IncludeModifier modifier = IncludeModifier.None,
            params TInclude[] revIncludes)
            where TResource : Hl7.Fhir.Model.Resource
            where TInclude : struct, Enum
        {
            if (revIncludes is null || revIncludes.Length == 0) return builder;
            return builder.RevInclude((IEnumerable<TInclude>)revIncludes, targetType, modifier);
        }
    }

}
