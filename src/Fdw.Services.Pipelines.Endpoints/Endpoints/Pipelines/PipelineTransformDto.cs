using System;
using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Endpoints;

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
}
