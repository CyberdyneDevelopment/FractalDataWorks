using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Response DTO surfacing the Aggregate-transform parameters on a pipeline detail response, read from
/// the composed aggregate's typed <c>GroupByFields</c>/<c>Aggregations</c> cascade children.
/// </summary>
public class AggregationDto
{
    /// <summary>Gets or sets the field names grouped by, in order.</summary>
    public IReadOnlyList<string> GroupByFields { get; set; } = [];

    /// <summary>Gets or sets the aggregations applied within each group.</summary>
    public IReadOnlyList<AggregationItemDto> Aggregations { get; set; } = [];
}
