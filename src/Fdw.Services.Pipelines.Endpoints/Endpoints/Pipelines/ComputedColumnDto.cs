namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Response DTO for a single computed column within a <see cref="CalculationDto"/>.
/// </summary>
public class ComputedColumnDto
{
    /// <summary>Gets or sets the output field name.</summary>
    public string OutputField { get; set; } = string.Empty;

    /// <summary>Gets or sets the formula/expression text.</summary>
    public string Formula { get; set; } = string.Empty;

    /// <summary>Gets or sets the formula language name.</summary>
    public string FormulaLanguage { get; set; } = string.Empty;
}
