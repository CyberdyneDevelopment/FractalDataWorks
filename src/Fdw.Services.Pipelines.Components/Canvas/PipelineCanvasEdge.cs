using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.UI.Abstractions.Canvas;

namespace Fdw.Services.Pipelines.Components.Canvas;

/// <summary>
/// A directed edge in a pipeline canvas graph.
/// </summary>
/// <remarks>
/// Carries per-mapping domain metadata in the <see cref="Metadata"/> dictionary using the keys
/// defined on <see cref="PipelineCanvasEdgeMetadataKeys"/> — used by Map-transform field-mapping
/// edges to hold the extras (<c>TargetType</c>, <c>TransformExpression</c>, <c>IsRequired</c>,
/// <c>DefaultValue</c>) that don't fit in the port-id-encoded source/destination field names.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class PipelineCanvasEdge : ICanvasEdge
{
    // Why: mirrors PipelineCanvasNode's mutable backing — kept separate so callers can write
    // individual keys without reconstructing the whole edge, while the public Metadata property
    // preserves the IReadOnlyDictionary<string,string> contract.
    private readonly Dictionary<string, string> _mutableMetadata;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineCanvasEdge"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this edge within the canvas.</param>
    /// <param name="sourceNodeId">The identifier of the source node.</param>
    /// <param name="targetNodeId">The identifier of the target node.</param>
    /// <param name="edgeType">The type of edge.</param>
    /// <param name="sourcePortId">The optional source port identifier.</param>
    /// <param name="targetPortId">The optional target port identifier.</param>
    /// <param name="label">The optional label displayed alongside the edge.</param>
    /// <param name="metadata">
    /// The optional metadata bag for domain-specific per-mapping properties. Null (the common case
    /// for a freshly-connected edge that has no extras yet) is stored as an empty bag, not a missing
    /// required value — an edge legitimately starts with no per-mapping overrides.
    /// </param>
    public PipelineCanvasEdge(
        string id,
        string sourceNodeId,
        string targetNodeId,
        ICanvasEdgeType edgeType,
        string? sourcePortId = null,
        string? targetPortId = null,
        string? label = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        Id = id;
        SourceNodeId = sourceNodeId;
        TargetNodeId = targetNodeId;
        EdgeType = edgeType;
        SourcePortId = sourcePortId;
        TargetPortId = targetPortId;
        Label = label;
        _mutableMetadata = metadata is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(metadata, StringComparer.Ordinal);
        Metadata = _mutableMetadata;
    }

    /// <inheritdoc />
    public string Id { get; }

    /// <inheritdoc />
    public string SourceNodeId { get; }

    /// <inheritdoc />
    public string TargetNodeId { get; }

    /// <inheritdoc />
    public ICanvasEdgeType EdgeType { get; }

    /// <inheritdoc />
    public string? SourcePortId { get; }

    /// <inheritdoc />
    public string? TargetPortId { get; }

    /// <inheritdoc />
    public string? Label { get; }

    /// <summary>
    /// Gets the per-mapping metadata bag for this edge (e.g. <c>TargetType</c>,
    /// <c>TransformExpression</c>, <c>IsRequired</c>, <c>DefaultValue</c> — see
    /// <see cref="PipelineCanvasEdgeMetadataKeys"/>). Concrete-only — not part of
    /// <see cref="ICanvasEdge"/>, mirroring how <see cref="PipelineCanvasNode.Metadata"/> carries
    /// domain-specific properties the render-agnostic contract layer doesn't interpret.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; }

    /// <summary>Gets the mutable metadata backing for use by <see cref="PipelineCanvasEditContext"/>.</summary>
    internal Dictionary<string, string> MutableMetadata => _mutableMetadata;
}
