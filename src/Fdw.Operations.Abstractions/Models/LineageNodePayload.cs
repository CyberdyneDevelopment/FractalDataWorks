using System;
using System.Collections.Generic;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Represents a node in the lineage graph.
/// </summary>
public sealed class LineageNodePayload
{
    /// <summary>Gets or sets the unique node identifier.</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>Gets or sets the display label.</summary>
    public string Label { get; set; } = string.Empty;
    /// <summary>Gets or sets the entity type.</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>Gets or sets the node category.</summary>
    public string? Category { get; set; }
    /// <summary>Gets or sets additional node properties.</summary>
    public IReadOnlyDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);
}
