using System;

namespace Fdw.Data.Abstractions.Rows;

/// <summary>What reading one column of a row found.</summary>
public readonly struct RowReading
{
    /// <summary>Initializes a new instance of the <see cref="RowReading"/> struct.</summary>
    /// <param name="found">Whether the row has the column.</param>
    /// <param name="value">The column's value; null when the column is absent or holds a null.</param>
    /// <param name="declaredType">The declared type of the entity property the value was read from; null for a dictionary row.</param>
    public RowReading(bool found, object? value, Type? declaredType)
    {
        Found = found;
        Value = value;
        DeclaredType = declaredType;
    }

    /// <summary>Gets whether the row has the column.</summary>
    public bool Found { get; }

    /// <summary>Gets the column's value; null when the column is absent or holds a null.</summary>
    public object? Value { get; }

    /// <summary>Gets the declared type of the entity property the value was read from; null for a dictionary row.</summary>
    public Type? DeclaredType { get; }
}
