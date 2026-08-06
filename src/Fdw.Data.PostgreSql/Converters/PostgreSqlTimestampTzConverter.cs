using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Converts PostgreSQL timestamp with time zone to CLR DateTimeOffset.
/// </summary>
[TypeOption(typeof(PostgreSqlConverters), "TimestampTz", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PostgreSqlTimestampTzConverter()
    : DataTypeConverterBase(
        id: 16,
        name: "TimestampTz",
        sourceType: "timestamptz",
        targetClrType: typeof(DateTimeOffset),
        dbType: DbType.DateTimeOffset)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        if (dbValue is DateTimeOffset dto)
        {
            return dto;
        }

        if (dbValue is DateTime dt)
        {
            return new DateTimeOffset(dt);
        }

        return new DateTimeOffset(Convert.ToDateTime(dbValue, CultureInfo.InvariantCulture));
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
