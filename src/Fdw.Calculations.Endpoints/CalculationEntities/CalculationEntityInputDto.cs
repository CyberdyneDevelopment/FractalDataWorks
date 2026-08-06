using System.Diagnostics.CodeAnalysis;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// DTO representing a single input declaration for a calculation entity.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CalculationEntityInputDto
{
    /// <summary>Gets or sets the alias identifying this input within the calculation.</summary>
    public string InputAlias { get; set; } = string.Empty;

    /// <summary>Gets or sets the input kind: "DataSet", "Container", or "Scalar".</summary>
    public string InputKind { get; set; } = "DataSet";

    /// <summary>Gets or sets the DataSet name (when InputKind is "DataSet").</summary>
    public string? DataSetName { get; set; }

    /// <summary>Gets or sets the connection name (when InputKind is "Container").</summary>
    public string? ConnectionName { get; set; }

    /// <summary>Gets or sets the container path (when InputKind is "Container").</summary>
    public string? ContainerPath { get; set; }

    /// <summary>Gets or sets the scalar value type name (when InputKind is "Scalar").</summary>
    public string? ScalarValueType { get; set; }

    /// <summary>Gets or sets the serialized scalar value (when InputKind is "Scalar").</summary>
    public string? ScalarSerializedValue { get; set; }
}
