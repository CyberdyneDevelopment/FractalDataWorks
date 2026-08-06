using System;
using Fdw.Data.DataContainers.Abstractions;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// Represents a calculated field that computes its value from other fields in a DataRow.
/// Calculated fields are not retrieved from data sources - they are computed post-query.
/// </summary>
public sealed class CalculatedDataField : IDataField
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalculatedDataField"/> class.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <param name="type">The .NET type of the calculated value.</param>
    /// <param name="calculator">Function that computes the value from a data row.</param>
    /// <param name="description">Optional description of what this field calculates.</param>
    public CalculatedDataField(
        string name,
        Type type,
        Func<IDataRow, object> calculator,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Field name cannot be empty", nameof(name));

        Name = name;
        FieldType = type ?? throw new ArgumentNullException(nameof(type));
        Calculator = calculator ?? throw new ArgumentNullException(nameof(calculator));
        Description = description;
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public Type FieldType { get; }

    /// <inheritdoc/>
    public bool IsKey => false;

    /// <inheritdoc/>
    public bool IsRequired => false;

    /// <inheritdoc/>
    public string? Description { get; }

    /// <inheritdoc/>
    public int? MaxLength => null;

    /// <inheritdoc/>
    public object? DefaultValue => null;

    /// <inheritdoc/>
    public bool IsCalculated => true;

    /// <inheritdoc/>
    public Func<IDataRow, object>? Calculator { get; }
}
