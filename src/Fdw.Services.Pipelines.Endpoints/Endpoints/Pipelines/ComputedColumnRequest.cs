using System.ComponentModel.DataAnnotations;
using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Request body for a single computed column within a <see cref="CalculationRequest"/>.
/// Maps onto one <c>PipelineTransformCalculationConfiguration</c> cascade-child row.
/// </summary>
public class ComputedColumnRequest : ICalculationSpec
{
    /// <summary>Gets or sets the output field name.</summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string OutputField { get; set; } = string.Empty;

    /// <summary>Gets or sets the formula/expression text.</summary>
    [Required]
    public string Formula { get; set; } = string.Empty;

}
