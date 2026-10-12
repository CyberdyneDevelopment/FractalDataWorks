using System;

namespace Fdw.Web.Calculations.Clients.Models;

/// <summary>
/// Edit model for creating or updating a calculation definition.
/// </summary>
public class CalculationEditModel : IEquatable<CalculationEditModel>
{
    /// <summary>
    /// Gets or sets the unique identifier (empty Guid for new calculations).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the calculation definition.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the target DataSet this calculation applies to.
    /// </summary>
    public string TargetDataSet { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the field where the calculation result is stored.
    /// </summary>
    public string ResultFieldName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the data type of the calculation result.
    /// </summary>
    public string ResultDataType { get; set; } = "decimal";

    /// <summary>
    /// Gets or sets the calculation formula expression.
    /// </summary>
    public string Formula { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional description of the calculation.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the calculation is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Creates a shallow clone of this edit model.
    /// </summary>
    public CalculationEditModel Clone() =>
        new()
        {
            Id = Id,
            Name = Name,
            TargetDataSet = TargetDataSet,
            ResultFieldName = ResultFieldName,
            ResultDataType = ResultDataType,
            Formula = Formula,
            Description = Description,
            IsEnabled = IsEnabled
        };

    /// <inheritdoc />
    public bool Equals(CalculationEditModel? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id
            && string.Equals(Name, other.Name, StringComparison.Ordinal)
            && string.Equals(TargetDataSet, other.TargetDataSet, StringComparison.Ordinal)
            && string.Equals(ResultFieldName, other.ResultFieldName, StringComparison.Ordinal)
            && string.Equals(ResultDataType, other.ResultDataType, StringComparison.Ordinal)
            && string.Equals(Formula, other.Formula, StringComparison.Ordinal)
            && string.Equals(Description, other.Description, StringComparison.Ordinal)
            && IsEnabled == other.IsEnabled;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as CalculationEditModel);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(Id, Name, TargetDataSet, ResultFieldName, ResultDataType, Formula, Description, IsEnabled);
}
