namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Specifies a field and sort direction used in the ORDER BY clause of a windowed calculation.
/// </summary>
public sealed class WindowOrderField
{
    /// <summary>Gets the name of the field to order by.</summary>
    public string FieldName { get; init; } = string.Empty;

    /// <summary>Gets a value indicating whether to sort descending.</summary>
    public bool Descending { get; init; }
}
