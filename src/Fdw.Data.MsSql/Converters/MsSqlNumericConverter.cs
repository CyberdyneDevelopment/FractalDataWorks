using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Converts SQL numeric to CLR Decimal.
/// </summary>
[TypeOption(typeof(MsSqlConverters), "Numeric", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MsSqlNumericConverter()
    : DataTypeConverterBase(
        id: 28,
        name: "Numeric",
        sourceType: "numeric",
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
