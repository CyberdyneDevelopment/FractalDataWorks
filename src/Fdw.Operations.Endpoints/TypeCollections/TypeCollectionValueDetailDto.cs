using System.Collections.Generic;
using Fdw.Operations.Endpoints.ConfigurationMetadata;

namespace Fdw.Operations.Endpoints.TypeCollections;

/// <summary>
/// Full metadata for a specific TypeOption, including configuration property details.
/// </summary>
public sealed class TypeCollectionValueDetailDto
{
    /// <summary>Gets or sets the TypeOption name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional expected configuration properties for this TypeOption.</summary>
    public IReadOnlyList<string> ExpectedProperties { get; set; } = [];

    /// <summary>Gets or sets the required configuration properties for this TypeOption.</summary>
    public IReadOnlyList<string> RequiredProperties { get; set; } = [];

    /// <summary>Gets or sets the full property metadata extracted from the configuration type.</summary>
    public IReadOnlyList<ConfigurationPropertyInfoDto> PropertyMetadata { get; set; } = [];
}
