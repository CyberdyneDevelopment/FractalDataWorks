using System;
using Fdw.Configuration.SourceGenerators.Models;

namespace Fdw.Configuration.SourceGenerators.Analysis;

/// <summary>
/// Maps C# types to SQL Server data types.
/// Default string type is varchar (not nvarchar).
/// </summary>
public static class SqlTypeMapper
{
    /// <summary>
    /// Default SQL type for strings (varchar, not nvarchar).
    /// </summary>
    public const string DefaultStringType = "varchar";

    /// <summary>
    /// Default max length for string columns without explicit MaxLength.
    /// </summary>
    public const int DefaultStringMaxLength = 500;

    /// <summary>
    /// Default precision for decimal columns.
    /// </summary>
    public const int DefaultDecimalPrecision = 18;

    /// <summary>
    /// Default scale for decimal columns.
    /// </summary>
    public const int DefaultDecimalScale = 2;

    /// <summary>
    /// Maps a property model to a SQL column definition.
    /// </summary>
    public static SqlColumnDefinition MapToColumn(PropertyModel property)
    {
        var column = new SqlColumnDefinition
        {
            ColumnName = property.ColumnName ?? property.PropertyName,
            IsNullable = property.IsNullable || !property.IsRequired,
            IsUnique = property.IsUnique,
            DefaultValue = property.DefaultValue
        };

        // Check for [DbType] override first
        if (property.DbTypeOverride != null)
        {
            ApplyDbTypeOverride(column, property.DbTypeOverride);
            return column;
        }

        // Handle complex types and collections as JSON
        if (property.IsComplexType || property.IsCollection)
        {
            column.SqlType = "varchar";
            column.MaxLength = -1; // MAX
            return column;
        }

        // Handle enums as their underlying type
        if (property.IsEnum)
        {
            var underlyingType = property.EnumUnderlyingType ?? "int";
            MapNumericType(column, underlyingType);
            return column;
        }

        // Map based on C# type
        MapCSharpType(column, property);

        return column;
    }

    private static void ApplyDbTypeOverride(SqlColumnDefinition column, DbTypeOverride dbType)
    {
        column.SqlType = dbType.SqlType;

        if (dbType.MaxLength.HasValue)
            column.MaxLength = dbType.MaxLength.Value;

        if (dbType.Precision.HasValue)
            column.Precision = dbType.Precision.Value;

        if (dbType.Scale.HasValue)
            column.Scale = dbType.Scale.Value;
    }

    // MA0051: Method length acceptable - comprehensive type mapping switch covering all C# primitives
#pragma warning disable MA0051 // Method is too long
    private static void MapCSharpType(SqlColumnDefinition column, PropertyModel property)
#pragma warning restore MA0051
    {
        // Normalize type name (remove nullable wrapper, namespace)
        var typeName = NormalizeTypeName(property.PropertyType);

        switch (typeName)
        {
            // String types - default to varchar (not nvarchar)
            case "string":
            case "String":
                column.SqlType = DefaultStringType;
                column.MaxLength = property.MaxLength ?? DefaultStringMaxLength;
                break;

            // Boolean
            case "bool":
            case "Boolean":
                column.SqlType = "bit";
                break;

            // Integer types
            case "byte":
            case "Byte":
                column.SqlType = "tinyint";
                break;

            case "short":
            case "Int16":
                column.SqlType = "smallint";
                break;

            case "int":
            case "Int32":
                column.SqlType = "int";
                break;

            case "long":
            case "Int64":
                column.SqlType = "bigint";
                break;

            // Unsigned integers (map to next larger signed type for safety)
            case "ushort":
            case "UInt16":
                column.SqlType = "int";
                break;

            case "uint":
            case "UInt32":
                column.SqlType = "bigint";
                break;

            case "ulong":
            case "UInt64":
                column.SqlType = "decimal";
                column.Precision = 20;
                column.Scale = 0;
                break;

            // Floating point
            case "float":
            case "Single":
                column.SqlType = "real";
                break;

            case "double":
            case "Double":
                column.SqlType = "float";
                break;

            case "decimal":
            case "Decimal":
                column.SqlType = "decimal";
                column.Precision = property.Precision ?? DefaultDecimalPrecision;
                column.Scale = property.Scale ?? DefaultDecimalScale;
                break;

            // Date/Time types
            case "DateTime":
                column.SqlType = "datetime2";
                column.Precision = 7;
                break;

            case "DateTimeOffset":
                column.SqlType = "datetimeoffset";
                column.Precision = 7;
                break;

            case "DateOnly":
                column.SqlType = "date";
                break;

            case "TimeOnly":
                column.SqlType = "time";
                column.Precision = 7;
                break;

            case "TimeSpan":
                column.SqlType = "bigint"; // Store as ticks
                break;

            // GUID
            case "Guid":
                column.SqlType = "uniqueidentifier";
                break;

            // Byte array
            case "byte[]":
            case "Byte[]":
                column.SqlType = "varbinary";
                column.MaxLength = property.MaxLength ?? -1; // MAX
                break;

            // Char - use varchar(1) not nchar
            case "char":
            case "Char":
                column.SqlType = "varchar";
                column.MaxLength = 1;
                break;

            // Default: store as JSON in varchar(max)
            default:
                column.SqlType = "varchar";
                column.MaxLength = -1; // MAX
                break;
        }
    }

    private static void MapNumericType(SqlColumnDefinition column, string typeName)
    {
        switch (typeName)
        {
            case "byte":
            case "Byte":
                column.SqlType = "tinyint";
                break;
            case "short":
            case "Int16":
                column.SqlType = "smallint";
                break;
            case "int":
            case "Int32":
                column.SqlType = "int";
                break;
            case "long":
            case "Int64":
                column.SqlType = "bigint";
                break;
            default:
                column.SqlType = "int";
                break;
        }
    }

    private static string NormalizeTypeName(string typeName)
    {
        // Handle nullable types
        if (typeName.EndsWith("?", StringComparison.Ordinal))
        {
            typeName = typeName.Substring(0, typeName.Length - 1);
        }

        // Handle System.Nullable<T>
        if (typeName.StartsWith("System.Nullable<", StringComparison.Ordinal))
        {
            typeName = typeName.Substring(16, typeName.Length - 17);
        }

        // Remove namespace
        var lastDot = typeName.LastIndexOf('.');
        if (lastDot >= 0)
        {
            typeName = typeName.Substring(lastDot + 1);
        }

        return typeName;
    }

    /// <summary>
    /// Gets the SQL default value expression for a C# default value.
    /// </summary>
    public static string? GetSqlDefaultValue(PropertyModel property)
    {
        if (property.DefaultValue == null)
            return null;

        var typeName = NormalizeTypeName(property.PropertyType);

        return typeName switch
        {
            "bool" or "Boolean" => string.Equals(property.DefaultValue, "true", StringComparison.OrdinalIgnoreCase) ? "1" : "0",
            "string" or "String" => $"'{EscapeSqlString(property.DefaultValue)}'",
            "Guid" => string.Equals(property.DefaultValue, "Guid.Empty", StringComparison.Ordinal) ? "'00000000-0000-0000-0000-000000000000'" : $"'{property.DefaultValue}'",
            "DateTime" => string.Equals(property.DefaultValue, "DateTime.UtcNow", StringComparison.Ordinal) ? "SYSUTCDATETIME()" : $"'{property.DefaultValue}'",
            _ => property.DefaultValue
        };
    }

    private static string EscapeSqlString(string value)
    {
        return value.Replace("'", "''");
    }
}
