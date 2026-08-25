namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// Client-side request for a single computed column within a <see cref="CalculationClientRequest"/>.
/// Field names mirror the server's <c>ComputedColumnRequest</c> exactly so the JSON round-trips.
/// </summary>
public class ComputedColumnClientRequest
{
    /// <summary>Gets or sets the output field name.</summary>
    public string OutputField { get; set; } = string.Empty;

    /// <summary>Gets or sets the formula/expression text.</summary>
    public string Formula { get; set; } = string.Empty;

}
