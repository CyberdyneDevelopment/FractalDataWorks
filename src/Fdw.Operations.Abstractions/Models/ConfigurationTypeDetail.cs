using System;
using System.Collections.Generic;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Detailed configuration type metadata including all properties.
/// </summary>
public sealed class ConfigurationTypeDetail
{
    /// <summary>Gets or sets the internal type name.</summary>
    public string TypeName { get; set; } = string.Empty;
    /// <summary>Gets or sets the user-friendly display name.</summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>Gets or sets the optional description.</summary>
    public string? Description { get; set; }
    /// <summary>Gets or sets the category.</summary>
    public string Category { get; set; } = string.Empty;
    /// <summary>Gets or sets the configurable properties for this type.</summary>
    public IReadOnlyList<ConfigurationPropertyInfo> Properties { get; set; } = Array.Empty<ConfigurationPropertyInfo>();
    /// <summary>Gets or sets any required capabilities.</summary>
    public IReadOnlyList<string>? RequiredCapabilities { get; set; }
    /// <summary>Gets or sets an optional documentation URL.</summary>
    public string? DocumentationUrl { get; set; }
}
