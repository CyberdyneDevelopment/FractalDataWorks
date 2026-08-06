using System.Collections.Generic;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Summary information about a configuration type for list displays.
/// </summary>
public sealed class ConfigurationTypeSummary
{
    /// <summary>Gets or sets the internal type name (e.g., "MsSql").</summary>
    public string TypeName { get; set; } = string.Empty;
    /// <summary>Gets or sets the user-friendly display name.</summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets the category (e.g., "Connection", "DataStore").</summary>
    public string Category { get; set; } = string.Empty;
    /// <summary>Gets or sets an optional icon identifier for UI rendering.</summary>
    public string? IconHint { get; set; }
    /// <summary>Gets or sets whether this configuration type is deprecated.</summary>
    public bool IsDeprecated { get; set; }
    /// <summary>
    /// Gets or sets the TypeCollections that provide valid values for properties on this type.
    /// Derived from [ValuesFrom] attributes on the configuration class.
    /// </summary>
    public IReadOnlyList<RelatedCollectionRef> RelatedCollections { get; set; } = [];
    /// <summary>Alias for <see cref="TypeName"/> used by ManagementUI.</summary>
    public string ServiceType => TypeName;
    /// <summary>Alias for <see cref="DisplayName"/> used by ManagementUI.</summary>
    public string Name => DisplayName;
}
