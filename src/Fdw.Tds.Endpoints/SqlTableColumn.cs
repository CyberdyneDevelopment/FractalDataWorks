using System;

namespace Fdw.Tds.Endpoints;

/// <summary>
/// One column of a table this endpoint exposes, as the closure describes it.
/// </summary>
/// <remarks>
/// The column states the .NET type its values hold; the endpoint chooses the SQL type from it. A column
/// whose .NET type the endpoint cannot write has no SQL type, and a table holding one cannot be described
/// to a client — it is reported, not skipped.
/// </remarks>
public sealed class SqlTableColumn
{
    /// <summary>Initializes a new instance of the <see cref="SqlTableColumn"/> class.</summary>
    /// <param name="name">The column's name.</param>
    /// <param name="ordinal">The column's position, from 1.</param>
    /// <param name="clrType">The .NET type its values hold, or null when the source declares none this endpoint knows.</param>
    /// <param name="declaredTypeName">The type name the source declared, for the error that names an unknown one.</param>
    /// <param name="isNullable">Whether it may hold NULL.</param>
    /// <param name="maxLength">The declared length of a text or binary column, or null when it declares none.</param>
    /// <param name="isKey">Whether it is part of the table's key.</param>
    /// <param name="isQueryable">Whether a query may filter and sort on it in the store.</param>
    /// <param name="isFilterable">Whether the source lets a caller filter on it (a data set field's own setting, FDW-1048).</param>
    public SqlTableColumn(string name, int ordinal, Type? clrType, string declaredTypeName, bool isNullable, int? maxLength, bool isKey, bool isQueryable, bool isFilterable)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Ordinal = ordinal;
        ClrType = clrType;
        DeclaredTypeName = declaredTypeName ?? throw new ArgumentNullException(nameof(declaredTypeName));
        IsNullable = isNullable;
        MaxLength = maxLength;
        IsKey = isKey;
        IsQueryable = isQueryable;
        IsFilterable = isFilterable;
    }

    /// <summary>Gets the column's name.</summary>
    public string Name { get; }

    /// <summary>Gets the column's position, from 1.</summary>
    public int Ordinal { get; }

    /// <summary>Gets the .NET type its values hold, or null when the source declares none this endpoint knows.</summary>
    public Type? ClrType { get; }

    /// <summary>Gets the type name the source declared.</summary>
    public string DeclaredTypeName { get; }

    /// <summary>Gets a value indicating whether it may hold NULL.</summary>
    public bool IsNullable { get; }

    /// <summary>Gets the declared length of a text or binary column, or null when it declares none.</summary>
    public int? MaxLength { get; }

    /// <summary>Gets a value indicating whether it is part of the table's key.</summary>
    public bool IsKey { get; }

    /// <summary>Gets a value indicating whether a query may filter and sort on it in the store.</summary>
    /// <remarks>A calculated column is computed after the rows are read, so the store cannot filter on it.</remarks>
    public bool IsQueryable { get; }

    /// <summary>Gets a value indicating whether the source lets a caller filter on it.</summary>
    /// <remarks>
    /// A WHERE that names a column the source has not made filterable is refused, as the HTTP routes refuse the same
    /// filter (FDW-1075). Ordering on it stays allowed there and here.
    /// </remarks>
    public bool IsFilterable { get; }
}
