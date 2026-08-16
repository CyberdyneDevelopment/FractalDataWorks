using System;
using Fdw.Data;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Internal entity for querying chain definitions for lineage tracking.
/// </summary>
[GenerateMapper]
public class ChainDefinitionLineageRecord
{
    /// <summary>Gets or sets the chain definition identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Gets or sets the chain definition name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the chain category.</summary>
    public string? Category { get; set; }
}
