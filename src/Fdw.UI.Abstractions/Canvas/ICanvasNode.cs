using System.Collections.Generic;

namespace Fdw.UI.Abstractions.Canvas;

/// <summary>
/// A node in the canvas graph.
/// </summary>
/// <remarks>
/// Nodes represent domain entities such as pipelines, datasets, calculations, or connections.
/// The <see cref="NodeType"/> carries the display name, category, and icon hint so renderers
/// can visualise the node without any switch/if-else on the type name.
/// </remarks>
public interface ICanvasNode
{
    /// <summary>
    /// Gets the unique identifier for this node within the canvas.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the node type, which carries display name, category, and icon metadata.
    /// </summary>
    ICanvasNodeType NodeType { get; }

    /// <summary>
    /// Gets the primary display label for the node.
    /// </summary>
    string Label { get; }

    /// <summary>
    /// Gets the optional secondary label shown beneath the primary label (e.g. type or schema).
    /// </summary>
    string? SubLabel { get; }

    /// <summary>
    /// Gets the optional status label (e.g. "Running", "Failed", "Healthy").
    /// </summary>
    /// <remarks>
    /// A null value means no status is rendered. The renderer maps this to its own status colours.
    /// </remarks>
    string? Status { get; }

    /// <summary>
    /// Gets the X coordinate of the node's top-left corner in canvas space.
    /// </summary>
    double X { get; }

    /// <summary>
    /// Gets the Y coordinate of the node's top-left corner in canvas space.
    /// </summary>
    double Y { get; }

    /// <summary>
    /// Gets the ports on this node through which edges connect.
    /// </summary>
    IReadOnlyList<ICanvasPort> Ports { get; }

    /// <summary>
    /// Gets the metadata bag for renderer-specific or domain-specific properties.
    /// </summary>
    /// <remarks>
    /// Keys and values are agreed by convention between the domain provider that builds the
    /// <see cref="ICanvasModel"/> and the renderer. The canvas contract layer does not interpret them.
    /// </remarks>
    IReadOnlyDictionary<string, string> Metadata { get; }
}
