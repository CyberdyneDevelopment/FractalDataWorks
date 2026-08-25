namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// Read-only surface for a single computed column within a Calculate transform request.
/// </summary>
public interface ICalculationSpec
{
    /// <summary>Gets the output field name.</summary>
    string OutputField { get; }

    /// <summary>Gets the formula/expression text.</summary>
    string Formula { get; }

}
