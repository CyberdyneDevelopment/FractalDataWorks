using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Converts PostgreSQL date to CLR DateOnly.
/// </summary>
[TypeOption(typeof(PostgreSqlConverters), "Date", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PostgreSqlDateConverter()
    : DataTypeConverterBase(
        id: 14,
        name: "Date",
        sourceType: "date",
        targetClrType: typeof(DateOnly),
        dbType: DbType.Date)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        if (dbValue is DateOnly dateOnly)
        {
            return dateOnly;
        }

        if (dbValue is DateTime dt)
        {
            return DateOnly.FromDateTime(dt);
        }

        return DateOnly.FromDateTime(Convert.ToDateTime(dbValue, System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        if (clrValue is DateOnly dateOnly)
        {
            return dateOnly.ToDateTime(TimeOnly.MinValue);
        }

        return clrValue;
    }
}
