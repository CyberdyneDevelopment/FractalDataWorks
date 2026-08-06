using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Request body for the Aggregate-transform parameters on a create/update pipeline transform. Maps
/// onto <c>PipelineTransformGroupByFieldConfiguration</c> + <c>PipelineTransformAggregationConfiguration</c>
/// cascade children via <see cref="Fdw.Services.Etl.Transforms.AggregateTransformType.MapSpecToConfiguration"/>.
/// </summary>
public class AggregationRequest
{
    /// <summary>Gets or sets the field names to group by, in order.</summary>
    [Required]
    public IReadOnlyList<string> GroupByFields { get; set; } = [];

    /// <summary>Gets or sets the aggregations to apply within each group.</summary>
    [Required]
    public IReadOnlyList<AggregationItemRequest> Aggregations { get; set; } = [];
}
