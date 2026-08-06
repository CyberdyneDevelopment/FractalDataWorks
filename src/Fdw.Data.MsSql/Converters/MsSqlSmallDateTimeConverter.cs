using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Converts SQL smalldatetime to CLR DateTime.
/// </summary>
[TypeOption(typeof(MsSqlConverters), "SmallDateTime", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MsSqlSmallDateTimeConverter()
    : DataTypeConverterBase(
        id: 14,
        name: "SmallDateTime",
        sourceType: "smalldatetime",
        targetClrType: typeof(DateTime),
        dbType: DbType.DateTime)
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
