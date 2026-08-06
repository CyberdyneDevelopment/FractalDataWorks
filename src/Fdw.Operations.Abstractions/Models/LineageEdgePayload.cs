using System;
using System.Collections.Generic;

namespace Fdw.Operations.Clients.Models;

/// <summary>
/// Represents an edge in the lineage graph.
/// </summary>
public sealed class LineageEdgePayload
{
    /// <summary>Gets or sets the source node identifier.</summary>
    public string SourceId { get; set; } = string.Empty;
    /// <summary>Gets or sets the target node identifier.</summary>
    public string TargetId { get; set; } = string.Empty;
    /// <summary>Gets or sets the type of relationship.</summary>
    public string Relation { get; set; } = string.Empty;
    /// <summary>Gets or sets additional edge properties.</summary>
    public IReadOnlyDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);
}
