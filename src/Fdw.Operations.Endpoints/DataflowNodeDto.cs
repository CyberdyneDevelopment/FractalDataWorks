using System;
using System.Reflection;
using System.Collections.Generic;
namespace Fdw.Operations.Endpoints;

/// <summary>
/// A node in the dataflow graph.
/// </summary>
public class DataflowNodeDto
{
    /// <summary>Gets or sets the unique node identifier.</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>Gets or sets the display label.</summary>
    public string Label { get; set; } = string.Empty;
    /// <summary>Gets or sets the node type (e.g., dataset, datastore, connection, source).</summary>
    public string NodeType { get; set; } = string.Empty;
    /// <summary>Gets or sets the optional category.</summary>
    public string? Category { get; set; }
    /// <summary>Gets or sets additional metadata key-value pairs.</summary>
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);
    /// <summary>Gets or sets the optional X coordinate for graph layout.</summary>
    public double? X { get; set; }
    /// <summary>Gets or sets the optional Y coordinate for graph layout.</summary>
    public double? Y { get; set; }
}