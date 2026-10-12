namespace Fdw.UI.Abstractions.Canvas;

/// <summary>
/// A directed edge connecting two nodes in the canvas graph.
/// </summary>
/// <remarks>
/// Edges represent relationships such as data flow, field-mapping wiring, or operation sequencing.
/// The optional port identifiers allow edges to connect to specific input/output ports on a node
/// rather than the node as a whole.
/// </remarks>
public interface ICanvasEdge
{
    /// <summary>
    /// Gets the unique identifier for this edge within the canvas.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the identifier of the source node.
    /// </summary>
    string SourceNodeId { get; }

    /// <summary>
    /// Gets the identifier of the target node.
    /// </summary>
    string TargetNodeId { get; }

    /// <summary>
    /// Gets the optional identifier of the source port on the source node.
    /// </summary>
    /// <remarks>
    /// Null means the edge connects to the node as a whole, not a specific port.
    /// </remarks>
    string? SourcePortId { get; }

    /// <summary>
    /// Gets the optional identifier of the target port on the target node.
    /// </summary>
    /// <remarks>
    /// Null means the edge connects to the node as a whole, not a specific port.
    /// </remarks>
    string? TargetPortId { get; }

    /// <summary>
    /// Gets the edge type, which carries display name and semantic meaning.
    /// </summary>
    ICanvasEdgeType EdgeType { get; }

    /// <summary>
    /// Gets the optional label displayed alongside the edge.
    /// </summary>
    string? Label { get; }
}
