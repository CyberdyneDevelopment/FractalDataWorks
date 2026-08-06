using System.Collections.Generic;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Represents a node in the dataflow graph.
/// </summary>
public sealed class DataflowNodeResponse
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>Gets or sets the display label.</summary>
    public string Label { get; set; } = string.Empty;
    /// <summary>Gets or sets the node type.</summary>
    public string NodeType { get; set; } = string.Empty;
    /// <summary>Gets or sets the node category.</summary>
    public string? Category { get; set; }
    /// <summary>Gets or sets additional metadata.</summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; set; }
}
