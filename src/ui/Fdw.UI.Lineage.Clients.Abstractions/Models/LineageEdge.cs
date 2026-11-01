namespace Fdw.UI.Lineage.Clients.Models;

/// <summary>
/// Represents a directed edge (dependency relationship) between two nodes in a lineage graph.
/// </summary>
public sealed class LineageEdge
{
    /// <summary>
    /// Gets or sets the unique identifier for this edge.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the source node.
    /// </summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the target node.
    /// </summary>
    public string TargetId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of relationship this edge represents.
    /// </summary>
    public ILineageEdgeType Type { get; set; } = LineageEdgeTypes.Produces;

    /// <summary>
    /// Gets or sets an optional display label for this edge.
    /// </summary>
    public string? Label { get; set; }
}
