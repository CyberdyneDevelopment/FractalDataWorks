namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// A typed scalar value used as a literal calculation input.
/// </summary>
public sealed class CalculationScalarValue
{
    /// <summary>Gets the scalar value type (e.g. Int32, Decimal, String).</summary>
    public IScalarValueType ValueType { get; init; } = null!;

    /// <summary>Gets the serialized string representation of the value.</summary>
    public string SerializedValue { get; init; } = string.Empty;
}
