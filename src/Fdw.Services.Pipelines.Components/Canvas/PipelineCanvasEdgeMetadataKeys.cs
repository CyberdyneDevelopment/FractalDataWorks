using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Components.Canvas;

/// <summary>
/// Convention constants for keys in <see cref="PipelineCanvasEdge.Metadata"/> on pipeline canvas
/// field-mapping edges.
/// </summary>
/// <remarks>
/// <para>
/// A Map-transform field mapping is represented by a <c>FieldMapping</c> edge whose
/// <c>SourcePortId</c>/<c>TargetPortId</c> encode the source/destination field names
/// (<c>in:{Field}</c>/<c>out:{Field}</c> — see <see cref="PipelineCanvasEdge"/>). The remaining
/// <see cref="PipelineFieldMappingClientRequest"/> properties that don't fit the port-id
/// encoding — display name, type coercion, transform expression, requiredness, default value —
/// live in this edge's <see cref="PipelineCanvasEdge.Metadata"/> bag under these keys.
/// </para>
/// <para>
/// The canvas contract layer does not interpret edge metadata — consumers (the properties panel,
/// <c>TransformConfigPayloadSerializer</c>) agree on these string keys by convention, mirroring
/// <see cref="PipelineCanvasMetadataKeys"/> for node metadata.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public static class PipelineCanvasEdgeMetadataKeys
{
    /// <summary>
    /// The mapping's display/identity name (<c>PipelineFieldMappingClientRequest.Name</c>).
    /// </summary>
    public const string MappingName = "MappingName";

    /// <summary>
    /// The optional target data type to coerce the value to (e.g. "long", "datetime").
    /// </summary>
    public const string TargetType = "TargetType";

    /// <summary>
    /// The optional named transform expression applied before <see cref="TargetType"/> coercion.
    /// </summary>
    public const string TransformExpression = "TransformExpression";

    /// <summary>
    /// Whether the source field is required. Stored as <c>"true"</c>/<c>"false"</c>; absent means
    /// not yet overridden from the DTO's own default (<c>false</c>).
    /// </summary>
    public const string IsRequired = "IsRequired";

    /// <summary>
    /// The default value applied when the source field is null or missing.
    /// </summary>
    public const string DefaultValue = "DefaultValue";

    /// <summary>
    /// Whether this mapping is enabled. Stored as <c>"true"</c>/<c>"false"</c>; absent means not yet
    /// overridden from the DTO's own default (<c>true</c>).
    /// </summary>
    public const string IsEnabled = "IsEnabled";
}
