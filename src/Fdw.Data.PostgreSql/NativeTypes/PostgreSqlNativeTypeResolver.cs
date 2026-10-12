using System;
using System.Collections.Generic;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Resolves PostgreSQL native type name strings from DDL to
/// <see cref="PostgreSqlNativeTypeBase"/> TypeCollection members.
/// </summary>
/// <remarks>
/// DDL and system catalogs store type names in PostgreSQL's lowercase form (e.g., "int4", "text").
/// <see cref="PostgreSqlNativeTypes"/> TypeCollection names match those lowercase names directly
/// (e.g., "int4", "text"). This resolver wraps <c>ByName</c> with a normalization map to handle
/// common aliases (e.g., "integer" → "int4", "boolean" → "bool").
/// </remarks>
public static class PostgreSqlNativeTypeResolver
{
    private static readonly Dictionary<string, string> _aliasMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["integer"]            = "int4",
            ["int"]                = "int4",
            ["bigint"]             = "int8",
            ["smallint"]           = "int2",
            ["boolean"]            = "bool",
            ["character varying"]  = "varchar",
            ["character"]          = "char",
            ["double precision"]   = "float8",
            ["real"]               = "float4",
            ["timestamp without time zone"] = "timestamp",
            ["timestamp with time zone"]    = "timestamptz",
            ["bytea"]              = "bytea",
            ["uuid"]               = "uuid",
        };

    /// <summary>
    /// Resolves a DDL native type name to its <see cref="PostgreSqlNativeTypeBase"/> TypeCollection member.
    /// </summary>
    /// <param name="dbNativeTypeName">
    /// The native type name as stored in DDL / pg_catalog (case-insensitive).
    /// </param>
    /// <returns>
    /// The matching <see cref="PostgreSqlNativeTypeBase"/>, or <see cref="PostgreSqlNativeTypes.NotFound"/>
    /// when the name cannot be resolved.
    /// </returns>
    public static PostgreSqlNativeTypeBase Resolve(string? dbNativeTypeName)
    {
        if (string.IsNullOrWhiteSpace(dbNativeTypeName))
            return (PostgreSqlNativeTypeBase)PostgreSqlNativeTypes.NotFound;

        var normalized = _aliasMap.TryGetValue(dbNativeTypeName, out var mapped)
            ? mapped
            : dbNativeTypeName;

        var found = (PostgreSqlNativeTypeBase)PostgreSqlNativeTypes.ByName(normalized);

        // Fallback: try original name in case it wasn't in our alias map.
        if (ReferenceEquals(found, PostgreSqlNativeTypes.NotFound)
            && !string.Equals(normalized, dbNativeTypeName, StringComparison.Ordinal))
        {
            found = (PostgreSqlNativeTypeBase)PostgreSqlNativeTypes.ByName(dbNativeTypeName);
        }

        return found;
    }
}
