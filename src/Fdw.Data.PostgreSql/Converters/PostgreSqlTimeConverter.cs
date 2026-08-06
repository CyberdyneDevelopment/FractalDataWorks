using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Converts PostgreSQL time (without time zone) to CLR TimeSpan.
/// </summary>
[TypeOption(typeof(PostgreSqlConverters), "Time", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PostgreSqlTimeConverter()
    : DataTypeConverterBase(
        id: 17,
        name: "Time",
        sourceType: "time",
        targetClrType: typeof(TimeSpan),
        dbType: DbType.Time)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        if (dbValue is TimeSpan ts)
        {
            return ts;
        }

        if (dbValue is TimeOnly timeOnly)
        {
            return timeOnly.ToTimeSpan();
        }

        if (dbValue is DateTime dt)
        {
            return dt.TimeOfDay;
        }

        return TimeSpan.Parse(dbValue.ToString()!, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
