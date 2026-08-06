using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Converts PostgreSQL timestamp (without time zone) to CLR DateTime.
/// </summary>
[TypeOption(typeof(PostgreSqlConverters), "Timestamp", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PostgreSqlTimestampConverter()
    : DataTypeConverterBase(
        id: 15,
        name: "Timestamp",
        sourceType: "timestamp",
        targetClrType: typeof(DateTime),
        dbType: DbType.DateTime2)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        return Convert.ToDateTime(dbValue, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
