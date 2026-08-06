using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Converts PostgreSQL interval to CLR TimeSpan.
/// </summary>
[TypeOption(typeof(PostgreSqlConverters), "Interval", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PostgreSqlIntervalConverter()
    : DataTypeConverterBase(
        id: 19,
        name: "Interval",
        sourceType: "interval",
        targetClrType: typeof(TimeSpan),
        dbType: DbType.Object)
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

        return TimeSpan.Parse(dbValue.ToString()!, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
