namespace Fdw.Data.Lineage;

/// <summary>
/// Represents an edge (relationship) in the lineage graph.
/// </summary>
public sealed class LineageEdge
{
    /// <summary>
    /// Unique identifier for the edge.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID of the source node.
    /// </summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>
    /// ID of the target node.
    /// </summary>
    public string TargetId { get; set; } = string.Empty;

    /// <summary>
    /// Type of relationship.
    /// </summary>
    public ILineageEdgeType Type { get; set; } = LineageEdgeTypes.NotFound;

    /// <summary>
    /// Optional label for the edge.
    /// </summary>
    public string? Label { get; set; }
}
