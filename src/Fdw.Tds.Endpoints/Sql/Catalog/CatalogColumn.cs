using System;
using Fdw.Protocols.Tds.Types;

namespace Fdw.Tds.Endpoints.Sql.Catalog;

/// <summary>
/// One column of a catalog view: its name, the SQL type SQL Server gives it, and whether it may be NULL.
/// </summary>
public sealed class CatalogColumn
{
    private CatalogColumn(string name, TdsColumnType type, bool isNullable)
    {
        Name = name;
        Type = type;
        IsNullable = isNullable;
    }

    /// <summary>Gets the column's name.</summary>
    public string Name { get; }

    /// <summary>Gets its SQL type.</summary>
    public TdsColumnType Type { get; }

    /// <summary>Gets a value indicating whether it may be NULL.</summary>
    public bool IsNullable { get; }

    /// <summary>A sysname column: nvarchar(128).</summary>
    /// <param name="name">The column's name.</param>
    /// <param name="nullable">Whether it may be NULL.</param>
    /// <returns>The column.</returns>
    public static CatalogColumn SysnameColumn(string name, bool nullable = false) => TextColumn(name, 128, nullable);

    /// <summary>An nvarchar(n) column.</summary>
    /// <param name="name">The column's name.</param>
    /// <param name="length">Its length in characters.</param>
    /// <param name="nullable">Whether it may be NULL.</param>
    /// <returns>The column.</returns>
    public static CatalogColumn TextColumn(string name, int length, bool nullable = false)
        => new(name, new TdsColumnType(SqlDataTypes.ByName("NVarChar"), length, 0, 0), nullable);

    /// <summary>An int column.</summary>
    /// <param name="name">The column's name.</param>
    /// <param name="nullable">Whether it may be NULL.</param>
    /// <returns>The column.</returns>
    public static CatalogColumn IntColumn(string name, bool nullable = false) => Of(name, "Int", nullable);

    /// <summary>A smallint column.</summary>
    /// <param name="name">The column's name.</param>
    /// <param name="nullable">Whether it may be NULL.</param>
    /// <returns>The column.</returns>
    public static CatalogColumn SmallIntColumn(string name, bool nullable = false) => Of(name, "SmallInt", nullable);

    /// <summary>A tinyint column.</summary>
    /// <param name="name">The column's name.</param>
    /// <param name="nullable">Whether it may be NULL.</param>
    /// <returns>The column.</returns>
    public static CatalogColumn TinyIntColumn(string name, bool nullable = false) => Of(name, "TinyInt", nullable);

    /// <summary>A bit column.</summary>
    /// <param name="name">The column's name.</param>
    /// <param name="nullable">Whether it may be NULL.</param>
    /// <returns>The column.</returns>
    public static CatalogColumn BitColumn(string name, bool nullable = false) => Of(name, "Bit", nullable);

    /// <summary>A datetime column, written as datetime2(3), which holds every datetime value.</summary>
    /// <param name="name">The column's name.</param>
    /// <param name="nullable">Whether it may be NULL.</param>
    /// <returns>The column.</returns>
    public static CatalogColumn DateTimeColumn(string name, bool nullable = false)
        => new(name, new TdsColumnType(SqlDataTypes.ByName("DateTime2"), 0, 0, 3), nullable);

    /// <summary>A varbinary(n) column.</summary>
    /// <param name="name">The column's name.</param>
    /// <param name="length">Its length in bytes.</param>
    /// <param name="nullable">Whether it may be NULL.</param>
    /// <returns>The column.</returns>
    public static CatalogColumn BinaryColumn(string name, int length, bool nullable = true)
        => new(name, new TdsColumnType(SqlDataTypes.ByName("VarBinary"), length, 0, 0), nullable);

    private static CatalogColumn Of(string name, string option, bool nullable)
        => new(name, new TdsColumnType(SqlDataTypes.ByName(option), 0, 0, 0), nullable);
}
