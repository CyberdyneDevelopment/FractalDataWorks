using System.Collections.Generic;

namespace Fdw.Operations.Endpoints.ConfigurationMetadata;

/// <summary>
/// Detailed configuration type metadata including all properties.
/// </summary>
public class ConfigurationTypeDetailDto
{
    /// <summary>
    /// Gets or sets the internal type name.
    /// </summary>
    public required string TypeName { get; set; }

    /// <summary>
    /// Gets or sets the user-friendly display name.
    /// </summary>
    public required string DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the category this type belongs to.
    /// </summary>
    public required string Category { get; set; }

    /// <summary>
    /// Gets or sets the configurable properties for this type.
    /// </summary>
    public required IReadOnlyList<ConfigurationPropertyInfoDto> Properties { get; set; }

    /// <summary>
    /// Gets or sets any required capabilities for this configuration type.
    /// </summary>
    public IReadOnlyList<string>? RequiredCapabilities { get; set; }

    /// <summary>
    /// Gets or sets an optional documentation URL.
    /// </summary>
    public string? DocumentationUrl { get; set; }
}
