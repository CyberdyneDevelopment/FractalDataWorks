using System;
using System.Collections.Generic;

namespace Fdw.Operations.Endpoints;

/// <summary>
/// Represents an edge in a lineage graph response, matching the LineageEdgePayload contract.
/// </summary>
public class LineageGraphEdgeResponse
{
    /// <summary>Gets or sets the identifier of the source node.</summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>Gets or sets the identifier of the target node.</summary>
    public string TargetId { get; set; } = string.Empty;

    /// <summary>Gets or sets the type of relationship (e.g., ReadsFrom, Consumes, ProducesDataSet).</summary>
    public string Relation { get; set; } = string.Empty;

    /// <summary>Gets or sets additional metadata properties for the edge.</summary>
    public IReadOnlyDictionary<string, object> Properties { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);
}
