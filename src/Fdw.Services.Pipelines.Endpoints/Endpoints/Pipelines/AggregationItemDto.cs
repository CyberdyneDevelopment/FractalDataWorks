namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Response DTO for a single aggregation within an <see cref="AggregationDto"/>.
/// </summary>
public class AggregationItemDto
{
    /// <summary>Gets or sets the source field that was aggregated.</summary>
    public string SourceField { get; set; } = string.Empty;

    /// <summary>Gets or sets the aggregate function name.</summary>
    public string Function { get; set; } = string.Empty;

    /// <summary>Gets or sets the output field name.</summary>
    public string OutputField { get; set; } = string.Empty;
}
