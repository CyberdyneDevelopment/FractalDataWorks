using System.Diagnostics.CodeAnalysis;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Request to validate a formula expression.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ValidateFormulaRequest
{
    /// <summary>Gets or sets the formula expression to validate.</summary>
    public string FormulaBody { get; set; } = string.Empty;

    /// <summary>Gets or sets the formula language (CSharp or Sql).</summary>
    public string FormulaLanguage { get; set; } = "CSharp";
}
