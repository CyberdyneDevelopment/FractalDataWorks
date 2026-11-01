namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// The resolved (evaluated) form of a <see cref="CalculationInput"/> after the input resolver has fetched or evaluated its value.
/// </summary>
public sealed class ResolvedCalculationInput
{
    /// <summary>Gets the alias identifying this input within the calculation.</summary>
    public string InputAlias { get; init; } = string.Empty;

    /// <summary>Gets the kind of input source (DataSet, Container, or Scalar).</summary>
    public ICalculationInputKind Kind { get; init; } = null!;

    /// <summary>Gets the resolved value (e.g. a DataTable, scalar, or container rows).</summary>
    public object? ResolvedValue { get; init; }
}
