using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Response DTO surfacing the Calculate-transform parameters on a pipeline detail response, read from
/// the composed aggregate's typed <c>Calculations</c> cascade children.
/// </summary>
public class CalculationDto
{
    /// <summary>Gets or sets the computed columns, in execution order.</summary>
    public IReadOnlyList<ComputedColumnDto> ComputedColumns { get; set; } = [];
}
