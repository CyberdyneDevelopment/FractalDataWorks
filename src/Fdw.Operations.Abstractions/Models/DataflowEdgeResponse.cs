using System.Collections.Generic;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Represents an edge connecting two nodes in the dataflow graph.
/// </summary>
public sealed class DataflowEdgeResponse
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>Gets or sets the source node identifier.</summary>
    public string Source { get; set; } = string.Empty;
    /// <summary>Gets or sets the target node identifier.</summary>
    public string Target { get; set; } = string.Empty;
    /// <summary>Gets or sets the relation type.</summary>
    public string RelationType { get; set; } = string.Empty;
    /// <summary>Gets or sets the display label.</summary>
    public string? Label { get; set; }
    /// <summary>Gets or sets additional metadata.</summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; set; }
}
