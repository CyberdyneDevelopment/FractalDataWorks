using System;
using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// DTO for a single pipeline transform step, as returned in pipeline detail responses.
/// </summary>
public class PipelineTransformDto
{
    /// <summary>Gets or sets the unique identifier for this transform step.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the transform name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the transform type (e.g., "Map", "Filter", "Calculate").</summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>Gets or sets the execution order within the pipeline.</summary>
    public int ExecutionOrder { get; set; }

    /// <summary>Gets or sets whether this transform step is enabled.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets the filter expression (for Filter transforms).</summary>
    public string? FilterExpression { get; set; }

    /// <summary>Gets or sets the aggregation parameters (for Aggregate transforms).</summary>
    public AggregationDto? Aggregation { get; set; }

    /// <summary>Gets or sets the lookup parameters (for Lookup transforms).</summary>
    public LookupDto? Lookup { get; set; }

    /// <summary>Gets or sets the calculation parameters (for Calculate transforms).</summary>
    public CalculationDto? Calculation { get; set; }

    /// <summary>Gets or sets the field mappings (for Map transforms).</summary>
    public IList<PipelineFieldMappingDto> FieldMappings { get; set; } = [];

    /// <summary>Gets or sets the fields a Sort transform orders by, most significant first.</summary>
    public IList<PipelineSortField>? Sort { get; set; }

    /// <summary>Gets or sets a LatestBy transform's parameters: key columns, order column, and which row of a key is kept.</summary>
    public PipelineLatestBy? LatestBy { get; set; }

    /// <summary>Gets or sets a Distinct transform's parameters: the key columns.</summary>
    public PipelineDistinct? Distinct { get; set; }

    /// <summary>Gets or sets the checks of a Validate stage, in the order they are asked.</summary>
    public IList<PipelineCheck>? Checks { get; set; }

    /// <summary>Gets or sets what a Validate stage does with a record that fails a check: Reject or Fail.</summary>
    public string? OnFail { get; set; }

    /// <summary>Gets or sets the paths of a Branch stage, in the order they are tried, each with its stages; null for any other stage.</summary>
    public IList<PipelinePathDto>? Paths { get; set; }

    /// <summary>Gets or sets what a Branch stage does with a record that matches no path: Pass or Reject; null for any other stage.</summary>
    public string? Unmatched { get; set; }

    /// <summary>Gets or sets the notification a Notify stage sends: the rule, the run point and the message; null for any other stage.</summary>
    public PipelineNotification? Notification { get; set; }

    /// <summary>Gets or sets what a Compute stage applies: the Built transformation and which field of the record feeds each of its inputs. Required for a Compute stage; refused on any other kind of stage.</summary>
    public PipelineCompute? Compute { get; set; }

    /// <summary>Gets or sets the name of the data set a Union stage appends after the last record the pipeline's source produces; null for any other stage.</summary>
    public string? UnionSource { get; set; }
}
