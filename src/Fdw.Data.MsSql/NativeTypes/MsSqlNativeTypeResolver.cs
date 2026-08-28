using Fdw.Data.Abstractions;
using System;
using System.Collections.Generic;

namespace Fdw.Data.MsSql;

/// <summary>
/// Resolves SQL Server native type name strings from DDL (lowercase) to
/// <see cref="DataTypeOptionBase"/> TypeCollection members (PascalCase).
/// </summary>
/// <remarks>
/// DDL stores type names in SQL Server's native lowercase form (e.g., "bigint", "nvarchar").
/// The <see cref="MsSqlNativeTypes"/> TypeCollection uses PascalCase names (e.g., "BigInt", "NVarChar")
/// because TypeOption names are C# identifiers. This resolver bridges the two naming conventions
/// via a pre-built mapping, then falls back to a case-insensitive ByName lookup.
/// </remarks>
public static class MsSqlNativeTypeResolver
{
    private static readonly Dictionary<string, string> _sqlToPascalMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["bigint"]           = "bigint",
            ["int"]              = "int",
            ["smallint"]         = "smallint",
            ["tinyint"]          = "tinyint",
            ["varchar"]          = "varchar",
            ["nvarchar"]         = "nvarchar",
            ["char"]             = "char",
            ["nchar"]            = "nchar",
            ["datetime2"]        = "datetime2",
            ["datetimeoffset"]   = "datetimeoffset",
            ["date"]             = "date",
            ["time"]             = "time",
            ["decimal"]          = "decimal",
            ["numeric"]          = "decimal",  // Why: SQL Server treats numeric as alias for decimal
            ["float"]            = "float",
            ["real"]             = "real",
            ["money"]            = "money",
            ["bit"]              = "bit",
            ["varbinary"]        = "varbinary",
            ["uniqueidentifier"] = "uniqueidentifier",
            ["binary"]           = "binary",
            ["text"]             = "text",
            ["ntext"]            = "ntext",
            ["image"]            = "image",
            ["xml"]              = "xml",
        };

    /// <summary>
    /// Resolves a DDL native type name to its <see cref="DataTypeOptionBase"/> TypeCollection member.
    /// </summary>
    /// <param name="dbNativeTypeName">
    /// The native type name as stored in DDL / system catalogs (case-insensitive, e.g., "bigint", "nvarchar").
    /// </param>
    /// <returns>
    /// The matching <see cref="DataTypeOptionBase"/>, or <see cref="MsSqlNativeTypes.NotFound"/>
    /// when the name cannot be resolved.
    /// </returns>
    public static DataTypeOptionBase Resolve(string? dbNativeTypeName)
    {
        if (string.IsNullOrWhiteSpace(dbNativeTypeName))
            return (DataTypeOptionBase)MsSqlNativeTypes.NotFound;

        var normalized = _sqlToPascalMap.TryGetValue(dbNativeTypeName, out var mapped)
            ? mapped
            : dbNativeTypeName;

        var found = (DataTypeOptionBase)MsSqlNativeTypes.ByName(normalized);

        if (ReferenceEquals(found, MsSqlNativeTypes.NotFound)
            && !string.Equals(normalized, dbNativeTypeName, StringComparison.Ordinal))
        {
            found = (DataTypeOptionBase)MsSqlNativeTypes.ByName(dbNativeTypeName);
        }

        return found;
    }
}
