using System;
using System.Reflection;
using System.Collections.Generic;
namespace Fdw.Operations.Endpoints;

/// <summary>
/// An edge (connection) in the dataflow graph.
/// </summary>
public class DataflowEdgeDto
{
    /// <summary>Gets or sets the unique edge identifier.</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>Gets or sets the source node identifier.</summary>
    public string Source { get; set; } = string.Empty;
    /// <summary>Gets or sets the target node identifier.</summary>
    public string Target { get; set; } = string.Empty;
    /// <summary>Gets or sets the relationship type (e.g., uses_source, stored_in, uses_connection).</summary>
    public string RelationType { get; set; } = string.Empty;
    /// <summary>Gets or sets the optional display label.</summary>
    public string? Label { get; set; }
    /// <summary>Gets or sets additional metadata key-value pairs.</summary>
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>(StringComparer.Ordinal);
}