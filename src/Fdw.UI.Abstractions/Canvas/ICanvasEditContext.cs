using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.UI.Abstractions.Canvas;

/// <summary>
/// Edit-mode command surface for a canvas.
/// </summary>
/// <remarks>
/// <para>
/// This interface is only accessible when <see cref="ICanvasModel.RenderMode"/> allows editing
/// (<see cref="RenderModeOptions.IRenderMode.AllowsEditing"/> is true). It is null in view-only canvases.
/// </para>
/// <para>
/// All operations return <see cref="IGenericResult"/> so failures surface through the standard
/// FDW result chain rather than exceptions. No Blazor EventCallback or ASP.NET types appear here —
/// the canvas contract layer is render-agnostic.
/// </para>
/// </remarks>
public interface ICanvasEditContext
{
    /// <summary>
    /// Adds a new node to the canvas at the specified position.
    /// </summary>
    /// <param name="nodeType">The type of node to add.</param>
    /// <param name="label">The display label for the new node.</param>
    /// <param name="x">The X coordinate of the node's initial position in canvas space.</param>
    /// <param name="y">The Y coordinate of the node's initial position in canvas space.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the new node's identifier on success.</returns>
    Task<IGenericResult<string>> AddNode(
        ICanvasNodeType nodeType,
        string label,
        double x,
        double y,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Connects two nodes (or specific ports) with a directed edge.
    /// </summary>
    /// <param name="sourceNodeId">The identifier of the source node.</param>
    /// <param name="targetNodeId">The identifier of the target node.</param>
    /// <param name="edgeType">The type of edge to create.</param>
    /// <param name="sourcePortId">The optional source port identifier. Null connects at the node level.</param>
    /// <param name="targetPortId">The optional target port identifier. Null connects at the node level.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result containing the new edge's identifier on success.</returns>
    Task<IGenericResult<string>> Connect(
        string sourceNodeId,
        string targetNodeId,
        ICanvasEdgeType edgeType,
        string? sourcePortId,
        string? targetPortId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a node to a new position in canvas space.
    /// </summary>
    /// <param name="nodeId">The identifier of the node to move.</param>
    /// <param name="x">The new X coordinate.</param>
    /// <param name="y">The new Y coordinate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult> MoveNode(
        string nodeId,
        double x,
        double y,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a node and all edges connected to it.
    /// </summary>
    /// <param name="nodeId">The identifier of the node to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult> DeleteNode(
        string nodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an edge from the canvas.
    /// </summary>
    /// <param name="edgeId">The identifier of the edge to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<IGenericResult> DeleteEdge(
        string edgeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Merges a set of metadata key-value pairs into the named node's metadata bag.
    /// </summary>
    /// <param name="nodeId">The identifier of the node whose metadata should be updated.</param>
    /// <param name="metadata">The key-value pairs to merge. Existing keys are overwritten; absent keys are unchanged.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure. Fails if the node is not found on the canvas.</returns>
    /// <remarks>
    /// This method is render-agnostic — no Blazor or ASP.NET types are referenced here.
    /// The caller is responsible for triggering any downstream re-render.
    /// </remarks>
    Task<IGenericResult> UpdateNodeMetadata(
        string nodeId,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken cancellationToken = default);
}
