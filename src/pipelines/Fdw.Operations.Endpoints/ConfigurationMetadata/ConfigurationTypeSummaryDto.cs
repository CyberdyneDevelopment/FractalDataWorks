using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata;

// ═══════════════════════════════════════════════════════════════════════════
// Response DTOs
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Summary information about a configuration type for list displays.
/// </summary>
public class ConfigurationTypeSummaryDto
{
    /// <summary>
    /// Gets or sets the internal type name (e.g., "MsSql").
    /// </summary>
    public required string TypeName { get; set; }

    /// <summary>
    /// Gets or sets the user-friendly display name.
    /// </summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the optional description of this configuration type.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the category this type belongs to (e.g., "Connection", "DataStore").
    /// </summary>
    public required string Category { get; set; }

    /// <summary>
    /// Gets or sets an optional icon identifier for UI rendering.
    /// </summary>
    public string? IconHint { get; set; }

    /// <summary>
    /// Gets or sets whether this configuration type is deprecated.
    /// </summary>
    public bool IsDeprecated { get; set; }

    /// <summary>
    /// Gets or sets the TypeCollections that provide valid values for properties on this type.
    /// Derived from [ValuesFrom] attributes on the configuration class.
    /// </summary>
    public IList<RelatedCollectionRefDto> RelatedCollections { get; set; } = [];
}

// ═══════════════════════════════════════════════════════════════════════════
// Request DTOs
// ═══════════════════════════════════════════════════════════════════════════

// ═══════════════════════════════════════════════════════════════════════════
// Mapping Utilities
// ═══════════════════════════════════════════════════════════════════════════
