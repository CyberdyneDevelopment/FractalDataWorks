using System;
using System.Collections.Generic;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Represents a node in a lineage graph response, matching the LineageNodePayload contract.
/// </summary>
public class LineageGraphNodeResponse
{
    /// <summary>Gets or sets the unique node identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the display label for the node.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the node type (e.g., DataSet, Pipeline, Connection, Calculation).</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional category for the node.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets additional metadata properties for the node.</summary>
    public IReadOnlyDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);
}
