using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using NpgsqlTypes;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Converts PostgreSQL point to CLR NpgsqlPoint.
/// </summary>
[TypeOption(typeof(PostgreSqlConverters), "Point", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PostgreSqlPointConverter()
    : DataTypeConverterBase(
        id: 28,
        name: "Point",
        sourceType: "point",
        targetClrType: typeof(NpgsqlPoint),
        dbType: DbType.Object)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        if (dbValue is NpgsqlPoint point)
        {
            return point;
        }

        throw new InvalidCastException($"Cannot convert {dbValue.GetType().Name} to NpgsqlPoint");
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
