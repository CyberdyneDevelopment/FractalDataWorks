#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using Fdw.Conventions;
using Fdw.Schema.Properties;

namespace Fdw.Schema.Ddl.MsSql;

/// <summary>
/// Maps .NET types to SQL Server data types.
/// </summary>
internal static class MsSqlTypeMapper
{
    /// <summary>
    /// Maps a .NET type to the appropriate SQL Server type string.
    /// </summary>
    /// <param name="property">The property definition to map.</param>
    /// <returns>The SQL Server type string (without length/precision modifiers).</returns>
    [ConventionOverride(MaxCyclomaticComplexity = 25)]  // Type mapping — comprehensive switch for CLR to SQL type mapping
    public static string MapToSqlType(IPropertyDefinition property)
    {
        // Check metadata for explicit SQL type
        if (TryGetFromMetadata<string>(property.Metadata, "SqlType", out var sqlType))
        {
            return sqlType;
        }

        // Check metadata for CLR type name
        if (!TryGetFromMetadata<string>(property.Metadata, "ClrType", out var clrTypeName))
        {
            // Default to string if no type information available
            return "VARCHAR";
        }

        var clrType = Type.GetType(clrTypeName);
        var underlyingType = clrType != null ? (Nullable.GetUnderlyingType(clrType) ?? clrType) : null;

        return underlyingType?.Name switch
        {
            nameof(String) => "VARCHAR",
            nameof(Int32) => "INT",
            nameof(Int64) => "BIGINT",
            nameof(Int16) => "SMALLINT",
            nameof(Byte) => "TINYINT",
            nameof(Boolean) => "BIT",
            nameof(Guid) => "UNIQUEIDENTIFIER",
            nameof(DateTime) => "DATETIME2",
            nameof(DateTimeOffset) => "DATETIMEOFFSET",
            nameof(Decimal) => "DECIMAL",
            nameof(Double) => "FLOAT",
            nameof(Single) => "REAL",
            nameof(TimeSpan) => "TIME",
            "Byte[]" => "VARBINARY",
            _ => "VARCHAR" // Default fallback
        };
    }

    /// <summary>
    /// Gets the max length for a property, defaulting based on type.
    /// </summary>
    public static int? GetMaxLength(IPropertyDefinition property)
    {
        if (TryGetFromMetadata<int>(property.Metadata, "MaxLength", out var maxLength))
        {
            return maxLength;
        }

        if (!TryGetFromMetadata<string>(property.Metadata, "ClrType", out var clrTypeName))
        {
            return -1; // Default to MAX for strings
        }

        var clrType = Type.GetType(clrTypeName);
        var underlyingType = clrType != null ? (Nullable.GetUnderlyingType(clrType) ?? clrType) : null;

        return underlyingType?.Name switch
        {
            nameof(String) => -1, // MAX
            "Byte[]" => -1, // MAX
            _ => null
        };
    }

    /// <summary>
    /// Gets the precision for numeric types.
    /// </summary>
    public static int? GetPrecision(IPropertyDefinition property)
    {
        if (TryGetFromMetadata<int>(property.Metadata, "Precision", out var precision))
        {
            return precision;
        }

        if (!TryGetFromMetadata<string>(property.Metadata, "ClrType", out var clrTypeName))
        {
            return null;
        }

        var clrType = Type.GetType(clrTypeName);
        var underlyingType = clrType != null ? (Nullable.GetUnderlyingType(clrType) ?? clrType) : null;

        return underlyingType?.Name switch
        {
            nameof(Decimal) => 18,
            _ => null
        };
    }

    /// <summary>
    /// Gets the scale for decimal types.
    /// </summary>
    public static int? GetScale(IPropertyDefinition property)
    {
        if (TryGetFromMetadata<int>(property.Metadata, "Scale", out var scale))
        {
            return scale;
        }

        if (!TryGetFromMetadata<string>(property.Metadata, "ClrType", out var clrTypeName))
        {
            return null;
        }

        var clrType = Type.GetType(clrTypeName);
        var underlyingType = clrType != null ? (Nullable.GetUnderlyingType(clrType) ?? clrType) : null;

        return underlyingType?.Name switch
        {
            nameof(Decimal) => 2,
            _ => null
        };
    }

    private static bool TryGetFromMetadata<T>(IReadOnlyDictionary<string, object>? metadata, string key, out T value)
    {
        value = default!;

        if (metadata == null || !metadata.TryGetValue(key, out var obj))
        {
            return false;
        }

        if (obj is T typedValue)
        {
            value = typedValue;
            return true;
        }

        return false;
    }
}
