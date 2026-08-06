using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Converts PostgreSQL array types to CLR Array.
/// </summary>
[TypeOption(typeof(PostgreSqlConverters), "Array", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PostgreSqlArrayConverter()
    : DataTypeConverterBase(
        id: 29,
        name: "Array",
        sourceType: "array",
        targetClrType: typeof(Array),
        dbType: DbType.Object)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        if (dbValue is Array array)
        {
            return array;
        }

        throw new InvalidCastException($"Cannot convert {dbValue.GetType().Name} to Array");
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
