using System.Diagnostics.CodeAnalysis;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Response from validating a formula expression.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ValidateFormulaResponse
{
    /// <summary>Gets or sets whether the formula is valid.</summary>
    public bool IsValid { get; set; }

    /// <summary>Gets or sets the validation error message if invalid.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Gets or sets the parsed field references found in the formula.</summary>
    public string[] FieldReferences { get; set; } = [];
}
