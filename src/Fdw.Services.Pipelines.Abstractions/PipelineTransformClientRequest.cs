using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// Client-side request describing a single transform (operation) step on a create/update-pipeline
/// request. Field names mirror the server's <c>CreatePipelineTransformRequest</c> exactly so the
/// JSON round-trips.
/// </summary>
/// <remarks>
/// Why: plain data only — no <c>ITransformOperationSpec</c> implementation here, to avoid this
/// client-abstractions package depending on <c>Fdw.Services.Etl.Abstractions</c> (which would create
/// a package cycle).
/// </remarks>
public class PipelineTransformClientRequest
{
    /// <summary>
    /// Gets or sets the transform step name (display/identity within the pipeline).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operation/transform type (Map, Filter, Calculate, Lookup, Aggregate).
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the execution order of this transform within the pipeline.
    /// </summary>
    public int ExecutionOrder { get; set; }

    /// <summary>
    /// Gets or sets the field mappings for this transform (used by Map transforms).
    /// </summary>
    public IList<PipelineFieldMappingClientRequest> FieldMappings { get; set; } = [];

    /// <summary>
    /// Gets or sets the aggregation parameters (used by Aggregate transforms).
    /// </summary>
    public AggregationClientRequest? Aggregation { get; set; }

    /// <summary>
    /// Gets or sets the lookup parameters (used by Lookup transforms).
    /// </summary>
    public LookupClientRequest? Lookup { get; set; }

    /// <summary>
    /// Gets or sets the calculation parameters (used by Calculate transforms).
    /// </summary>
    public CalculationClientRequest? Calculation { get; set; }

    /// <summary>
    /// Gets or sets the filter expression (used by Filter transforms).
    /// </summary>
    public string? FilterExpression { get; set; }
}
