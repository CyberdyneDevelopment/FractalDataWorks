using System;
using Fdw.Protocols.Tds.Types;

namespace Fdw.Protocols.Tds;

/// <summary>
/// One column of a result set: its name, its SQL type and whether it may hold NULL.
/// </summary>
public sealed class TdsColumn
{
    /// <summary>The longest column name TDS carries.</summary>
    public const int MaxNameLength = 128;

    /// <summary>Initializes a new instance of the <see cref="TdsColumn"/> class.</summary>
    /// <param name="name">The column's name; at most 128 characters.</param>
    /// <param name="type">The column's SQL type and facets.</param>
    /// <param name="isNullable">Whether the column may hold NULL.</param>
    public TdsColumn(string name, TdsColumnType type, bool isNullable)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(name.Length, MaxNameLength, nameof(name));
        Type = type ?? throw new ArgumentNullException(nameof(type));
        IsNullable = isNullable;
    }

    /// <summary>Gets the column's name.</summary>
    public string Name { get; }

    /// <summary>Gets the column's SQL type and facets.</summary>
    public TdsColumnType Type { get; }

    /// <summary>Gets a value indicating whether the column may hold NULL.</summary>
    public bool IsNullable { get; }
}
