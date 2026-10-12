namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// Client-side request for a single aggregation within an <see cref="AggregationClientRequest"/>.
/// Field names mirror the server's <c>AggregationItemRequest</c> exactly so the JSON round-trips.
/// </summary>
public class AggregationItemClientRequest
{
    /// <summary>Gets or sets the source field to aggregate.</summary>
    public string SourceField { get; set; } = string.Empty;

    /// <summary>Gets or sets the aggregate function name, resolved against <c>AggregateFunctions</c>.</summary>
    public string Function { get; set; } = string.Empty;

    /// <summary>Gets or sets the output field name.</summary>
    public string OutputField { get; set; } = string.Empty;
}
