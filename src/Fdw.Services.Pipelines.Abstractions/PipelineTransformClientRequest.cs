using System;
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
    /// Gets or sets the step's identifier when this value describes a stored step, as in a pipeline
    /// detail read. Null on a create or update request, where the server assigns it; the server's request type has no id
    /// and ignores the null.
    /// </summary>
    public Guid? Id { get; set; }

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

    /// <summary>Gets or sets whether the step runs. Enabled unless the request says otherwise.</summary>
    public bool IsEnabled { get; set; } = true;

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

    /// <summary>Gets or sets the fields a Sort transform orders by, most significant first.</summary>
    public IList<PipelineSortField>? Sort { get; set; }

    /// <summary>Gets or sets a LatestBy transform's parameters: key columns, order column, and which row of a key is kept.</summary>
    public PipelineLatestBy? LatestBy { get; set; }

    /// <summary>Gets or sets a Distinct transform's parameters: the key columns.</summary>
    public PipelineDistinct? Distinct { get; set; }

    /// <summary>Gets or sets the checks a Validate stage asks of every record, in order (at least one for a Validate stage).</summary>
    public IList<PipelineCheck>? Checks { get; set; }

    /// <summary>
    /// Gets or sets what a Validate stage does with a record that fails a check: <c>Reject</c> (the record leaves the stream
    /// and is reported with the check's name) or <c>Fail</c> (the run fails). Required for a Validate stage, no default.
    /// </summary>
    public string? OnFail { get; set; }

    /// <summary>Gets or sets the paths of a Branch stage, in the order they are tried; the first whose condition a record meets takes it.</summary>
    public IList<PipelinePathClientRequest>? Paths { get; set; }

    /// <summary>Gets or sets what a Branch stage does with a record that matches no path: Pass or Reject.</summary>
    public string? Unmatched { get; set; }

    /// <summary>Gets or sets the notification a Notify stage sends: the rule, the run point and the message. Required for a Notify stage.</summary>
    public PipelineNotification? Notification { get; set; }

    /// <summary>Gets or sets what a Compute stage applies: the Built transformation and which field of the record feeds each of its inputs. Required for a Compute stage; refused on any other kind of stage.</summary>
    public PipelineCompute? Compute { get; set; }

    /// <summary>Gets or sets the name of the data set a Union stage appends after the last record the pipeline's source produces; its field mappings say how that data set's fields become the stage's output fields.</summary>
    public string? UnionSource { get; set; }
}
