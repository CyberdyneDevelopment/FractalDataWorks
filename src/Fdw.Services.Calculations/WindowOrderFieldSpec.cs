namespace Fdw.Services.Calculations;

/// <summary>
/// Specification for an order-by field in a windowed calculation.
/// </summary>
/// <param name="FieldName">The field name to order by.</param>
/// <param name="Descending">Whether to sort descending.</param>
internal sealed record WindowOrderFieldSpec(string FieldName, bool Descending);
