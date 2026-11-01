namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Describes where a calculation result should be written and what field/type it produces.
/// </summary>
public sealed class CalculationOutputSpec
{
    /// <summary>Gets the name of the DataSet that receives the calculation output.</summary>
    public string OutputDataSetName { get; init; } = string.Empty;

    /// <summary>Gets the name of the field that will hold the result.</summary>
    public string ResultFieldName { get; init; } = string.Empty;

    /// <summary>Gets the data type name of the result field (e.g. "Decimal").</summary>
    public string ResultDataTypeName { get; init; } = "Decimal";
}
