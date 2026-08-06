using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.UI.Abstractions.Canvas;

namespace Fdw.Services.Pipelines.Components.Canvas;

/// <summary>
/// A node in a pipeline canvas graph.
/// </summary>
/// <remarks>
/// Carries domain-specific metadata in the <see cref="Metadata"/> dictionary using the keys
/// defined on <see cref="PipelineCanvasMetadataKeys"/>.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class PipelineCanvasNode : ICanvasNode
{
    // Why: mutable backing is kept separate so ICanvasEditContext.UpdateNodeMetadata can
    // write individual keys without reconstructing the whole node, while the public
    // Metadata property preserves the IReadOnlyDictionary<string,string> contract.
    private readonly Dictionary<string, string> _mutableMetadata;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineCanvasNode"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this node within the canvas.</param>
    /// <param name="nodeType">The type of this node.</param>
    /// <param name="label">The primary display label.</param>
    /// <param name="subLabel">The optional secondary label.</param>
    /// <param name="x">The X coordinate in canvas space.</param>
    /// <param name="y">The Y coordinate in canvas space.</param>
    /// <param name="ports">The ports on this node.</param>
    /// <param name="metadata">The metadata bag for domain-specific properties.</param>
    public PipelineCanvasNode(
        string id,
        ICanvasNodeType nodeType,
        string label,
        string? subLabel,
        double x,
        double y,
        IReadOnlyList<ICanvasPort> ports,
        IReadOnlyDictionary<string, string> metadata)
    {
        Id = id;
        NodeType = nodeType;
        Label = label;
        SubLabel = subLabel;
        X = x;
        Y = y;
        Ports = ports;
        // Why: copy to a mutable dictionary so the edit context can write keys via
        // MutableMetadata without violating the public IReadOnlyDictionary contract.
        _mutableMetadata = new Dictionary<string, string>(metadata, StringComparer.Ordinal);
        Metadata = _mutableMetadata;
    }

    /// <summary>
    /// Replaces this node's ports wholesale.
    /// </summary>
    /// <remarks>
    /// Used by <see cref="PipelineCanvasEditContext.PopulateTransformPorts"/> to repopulate a
    /// Transform node's ports once the bound source/sink field lists are known — a Transform node
    /// gets its symmetric in/out ports at <c>AddNode</c> time, before any field binding exists.
    /// </remarks>
    /// <param name="ports">The replacement port list.</param>
    internal void SetPorts(IReadOnlyList<ICanvasPort> ports) => Ports = ports;

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public ICanvasNodeType NodeType { get; }

    /// <inheritdoc />
    public string Label { get; }

    /// <inheritdoc />
    public string? SubLabel { get; }

    /// <inheritdoc />
    public string? Status => null;

    /// <inheritdoc />
    public double X { get; internal set; }

    /// <inheritdoc />
    public double Y { get; internal set; }

    /// <inheritdoc />
    public IReadOnlyList<ICanvasPort> Ports { get; private set; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>Gets the mutable metadata backing for use by <see cref="PipelineCanvasEditContext"/>.</summary>
    internal Dictionary<string, string> MutableMetadata => _mutableMetadata;
}
