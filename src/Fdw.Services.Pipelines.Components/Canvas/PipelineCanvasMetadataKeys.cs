using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Pipelines.Components.Canvas;

/// <summary>
/// Convention constants for keys in <c>ICanvasNode.Metadata</c> on pipeline canvas nodes.
/// </summary>
/// <remarks>
/// The canvas contract layer does not interpret metadata — consumers and renderers agree on
/// these string keys by convention. All values are persisted as strings in the metadata dictionary.
/// </remarks>
[ExcludeFromCodeCoverage]
public static class PipelineCanvasMetadataKeys
{
    // ── DataSet nodes (source and sink) ──────────────────────────────────────

    /// <summary>
    /// The logical name of the DataSet this node represents.
    /// </summary>
    public const string DataSetName = "DataSetName";

    /// <summary>
    /// The logical identifier (GUID string) of the DataSet this node represents.
    /// Stored as a GUID string (e.g. <c>"00000000-0000-0000-0000-000000000000"</c>).
    /// </summary>
    public const string DataSetId = "DataSetId";

    /// <summary>
    /// The name of the connection associated with this DataSet node
    /// (<c>SourceConnectionName</c> or <c>DestinationConnectionName</c>).
    /// </summary>
    public const string ConnectionName = "ConnectionName";

    /// <summary>
    /// Discriminates source vs sink DataSet nodes. Value is <c>"Source"</c> or <c>"Sink"</c>.
    /// </summary>
    public const string DataSetRole = "DataSetRole";

    // ── Transform nodes ───────────────────────────────────────────────────────

    /// <summary>
    /// The <c>OperationType</c> string from <c>PipelineTransformConfiguration</c>
    /// (e.g. "Map", "Filter", "Calculate", "Lookup", "Aggregate").
    /// </summary>
    public const string OperationType = "OperationType";

    /// <summary>
    /// The execution order integer (1-based) from <c>PipelineTransformConfiguration.ExecutionOrder</c>,
    /// stored as a string.
    /// </summary>
    public const string ExecutionOrder = "ExecutionOrder";

    /// <summary>
    /// The logical identifier (GUID string) of the <c>PipelineTransformConfiguration</c> this node
    /// represents. Empty when the transform has not yet been persisted.
    /// </summary>
    public const string TransformId = "TransformId";

    /// <summary>
    /// Serialised JSON payload carrying the per-operation sub-configuration
    /// (field mappings, filter expression, calculation, aggregation, or lookup config).
    /// Null when there is no sub-configuration.
    /// </summary>
    public const string ConfigPayload = "ConfigPayload";

    // ── DataSetRole values ────────────────────────────────────────────────────

    /// <summary>Value for <see cref="DataSetRole"/> on a source node.</summary>
    public const string RoleSource = "Source";

    /// <summary>Value for <see cref="DataSetRole"/> on a sink node.</summary>
    public const string RoleSink = "Sink";
}
