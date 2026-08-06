using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// Client-side request for the Aggregate-transform parameters on a create/update pipeline transform.
/// Field names mirror the server's <c>AggregationRequest</c> exactly so the JSON round-trips.
/// </summary>
public class AggregationClientRequest
{
    /// <summary>Gets or sets the field names to group by, in order.</summary>
    public IReadOnlyList<string> GroupByFields { get; set; } = [];

    /// <summary>Gets or sets the aggregations to apply within each group.</summary>
    public IReadOnlyList<AggregationItemClientRequest> Aggregations { get; set; } = [];
}
