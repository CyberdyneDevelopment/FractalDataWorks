using System.Collections.Generic;

namespace Fdw.Operations.Endpoints.TypeCollections;

/// <summary>
/// Summary of a TypeOption within a TypeCollection.
/// </summary>
public sealed class TypeCollectionValueSummaryDto
{
    /// <summary>Gets or sets the TypeOption name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional expected configuration properties for this TypeOption.</summary>
    public IReadOnlyList<string> ExpectedProperties { get; set; } = [];

    /// <summary>Gets or sets the required configuration properties for this TypeOption.</summary>
    public IReadOnlyList<string> RequiredProperties { get; set; } = [];
}
