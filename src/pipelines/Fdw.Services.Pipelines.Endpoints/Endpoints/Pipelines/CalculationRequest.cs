using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Request body for the Calculate-transform parameters on a create/update pipeline transform. Maps
/// onto the <c>PipelineTransformCalculationConfiguration</c> cascade children via
/// <see cref="Fdw.Services.Etl.Transforms.CalculateTransformType.MapSpecToConfiguration"/>.
/// </summary>
public class CalculationRequest
{
    /// <summary>Gets or sets the computed columns to evaluate, in execution order.</summary>
    [Required]
    public IReadOnlyList<ComputedColumnRequest> ComputedColumns { get; set; } = [];
}
