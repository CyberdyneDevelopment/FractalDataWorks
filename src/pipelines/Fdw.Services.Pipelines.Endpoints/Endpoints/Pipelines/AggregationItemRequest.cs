using System.ComponentModel.DataAnnotations;
using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Request body for a single aggregation within an <see cref="AggregationRequest"/>.
/// Maps onto one <c>PipelineTransformAggregationConfiguration</c> cascade-child row.
/// </summary>
public class AggregationItemRequest : IAggregationSpec
{
    /// <summary>Gets or sets the source field to aggregate.</summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string SourceField { get; set; } = string.Empty;

    /// <summary>Gets or sets the aggregate function name, resolved against <c>AggregateFunctions</c>.</summary>
    [Required]
    public string Function { get; set; } = string.Empty;

    /// <summary>Gets or sets the output field name.</summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string OutputField { get; set; } = string.Empty;
}
