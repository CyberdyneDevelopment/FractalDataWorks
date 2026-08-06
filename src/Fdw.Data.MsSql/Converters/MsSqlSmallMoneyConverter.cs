using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Converts SQL smallmoney to CLR Decimal.
/// </summary>
[TypeOption(typeof(MsSqlConverters), "SmallMoney", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MsSqlSmallMoneyConverter()
    : DataTypeConverterBase(
        id: 19,
        name: "SmallMoney",
        sourceType: "smallmoney",
        targetClrType: typeof(decimal),
        dbType: DbType.Currency)
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
