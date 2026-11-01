namespace Fdw.Services.Calculations.Abstractions;

/// <summary>
/// Declares a named input to a calculation entity, specifying its kind and source.
/// </summary>
public sealed class CalculationInput
{
    /// <summary>Gets the kind of this input (DataSet, Container, or Scalar).</summary>
    public ICalculationInputKind Kind { get; init; } = null!;

    /// <summary>Gets the DataSet name when <see cref="Kind"/> is DataSet; otherwise <see langword="null"/>.</summary>
    public string? DataSetName { get; init; }

    /// <summary>Gets the connection name when <see cref="Kind"/> is Container; otherwise <see langword="null"/>.</summary>
    public string? ConnectionName { get; init; }

    /// <summary>Gets the container path when <see cref="Kind"/> is Container; otherwise <see langword="null"/>.</summary>
    public string? ContainerPath { get; init; }

    /// <summary>Gets the scalar value when <see cref="Kind"/> is Scalar; otherwise <see langword="null"/>.</summary>
    public CalculationScalarValue? ScalarValue { get; init; }

    /// <summary>Gets the alias that identifies this input within the calculation.</summary>
    public string InputAlias { get; init; } = string.Empty;

    /// <summary>Creates a DataSet-sourced calculation input.</summary>
    public static CalculationInput FromDataSet(string dataSetName, string alias)
        => new() { Kind = CalculationInputKinds.ByName("DataSet"),
                   DataSetName = dataSetName, InputAlias = alias };

    /// <summary>Creates a Container-sourced calculation input.</summary>
    public static CalculationInput FromContainer(string connectionName, string containerPath, string alias)
        => new() { Kind = CalculationInputKinds.ByName("Container"),
                   ConnectionName = connectionName, ContainerPath = containerPath, InputAlias = alias };

    /// <summary>Creates a Scalar-value calculation input.</summary>
    public static CalculationInput FromScalar(CalculationScalarValue scalar, string alias)
        => new() { Kind = CalculationInputKinds.ByName("Scalar"),
                   ScalarValue = scalar, InputAlias = alias };
}
