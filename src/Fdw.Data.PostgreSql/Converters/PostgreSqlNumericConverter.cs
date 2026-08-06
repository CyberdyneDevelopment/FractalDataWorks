using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Converts PostgreSQL decimal (alias for numeric) to CLR Decimal.
/// </summary>
[TypeOption(typeof(PostgreSqlConverters), "Numeric", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PostgreSqlNumericConverter()
    : DataTypeConverterBase(
        id: 30,
        name: "Numeric",
        sourceType: "decimal",
        targetClrType: typeof(decimal),
        dbType: DbType.Decimal)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        return Convert.ToDecimal(dbValue, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
