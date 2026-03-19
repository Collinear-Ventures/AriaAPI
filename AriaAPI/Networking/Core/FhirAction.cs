// Copyright (c) 2025-2026 Dominic DiCostanzo. Licensed under AGPL-3.0.
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AriaAPI.Networking.Core
{
    /// <summary>
    /// Indicates the high-level FHIR action to be performed when creating a resource.
    /// The underlying resources typically expect the string code (e.g., "read").
    /// </summary>
    public enum FhirAction
    {
        /// <summary>Read action (default for most resources in this API).</summary>
        Read = 0,
        /// <summary>Search action (if your resource constructors support it).</summary>
        Search = 1,
        /// <summary>Update action (if your resource constructors support it).</summary>
        Update = 2,
        /// <summary>Delete action (if your resource constructors support it).</summary>
        Delete = 3,
        /// <summary>Create action (if your resource constructors support it).</summary>
        Create = 4,
        /// <summary>$expand action (if your resource constructors support it).</summary>
        Expand = 5,
        /// <summary>Mark as Exported action (if your resource constructors support it).</summary>
        MarkAsExported = 6,
        /// <summary>$checkin action (if your resource constructors support it).</summary>
        CheckIn = 7,
        /// <summary>$checkout action (if your resource constructors support it).</summary>
        CheckOut = 8

    }
}
