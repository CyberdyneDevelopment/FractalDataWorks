using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Converts PostgreSQL time with time zone to CLR DateTimeOffset.
/// </summary>
[TypeOption(typeof(PostgreSqlConverters), "TimeTz", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PostgreSqlTimeTzConverter()
    : DataTypeConverterBase(
        id: 18,
        name: "TimeTz",
        sourceType: "time with time zone",
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

        return DateTimeOffset.Parse(dbValue.ToString()!, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
