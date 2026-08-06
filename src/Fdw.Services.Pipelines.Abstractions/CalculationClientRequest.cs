using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// Client-side request for the Calculate-transform parameters on a create/update pipeline transform.
/// Field names mirror the server's <c>CalculationRequest</c> exactly so the JSON round-trips.
/// </summary>
public class CalculationClientRequest
{
    /// <summary>Gets or sets the computed columns to evaluate, in execution order.</summary>
    public IReadOnlyList<ComputedColumnClientRequest> ComputedColumns { get; set; } = [];
}
