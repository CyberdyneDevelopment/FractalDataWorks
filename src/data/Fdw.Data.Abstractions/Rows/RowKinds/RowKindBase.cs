using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Data.Abstractions.Rows;

/// <summary>The base of every row shape.</summary>
[ExcludeFromCodeCoverage]
public abstract class RowKindBase : TypeOptionBase<RowKindBase>, IRowKind
{
    /// <summary>Initializes a new instance of the <see cref="RowKindBase"/> class.</summary>
    /// <param name="name">The shape's name.</param>
    protected RowKindBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Holds(object row);

    /// <inheritdoc />
    public abstract RowReading Read(object row, string column);

    /// <inheritdoc />
    public abstract Type? BindingType(RowReading reading, Type columnType);

    /// <summary>Finds a column among a dictionary's keys: the exact key first, which is the common case and one lookup, otherwise the key that differs only by case.</summary>
    /// <param name="keys">The dictionary's keys.</param>
    /// <param name="read">Reads the value of a key.</param>
    /// <param name="column">The column name.</param>
    /// <returns>What the read found.</returns>
    protected static RowReading Find(IEnumerable<string> keys, Func<string, object?> read, string column)
    {
        string? insensitive = null;
        foreach (var key in keys)
        {
            if (string.Equals(key, column, StringComparison.Ordinal))
                return new RowReading(true, read(key), null);

            if (insensitive is null && string.Equals(key, column, StringComparison.OrdinalIgnoreCase))
                insensitive = key;
        }

        return insensitive is null ? new RowReading(false, null, null) : new RowReading(true, read(insensitive), null);
    }

    /// <summary>Gives the binding type of a dictionary row: the column's for a null it holds, none for a value or an absent column.</summary>
    /// <param name="reading">What the read found.</param>
    /// <param name="columnType">The column's CLR type.</param>
    /// <returns>The type, or null.</returns>
    protected static Type? DictionaryBinding(RowReading reading, Type columnType) =>
        reading.Found && reading.Value is null ? columnType : null;
}
