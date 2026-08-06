using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Converts SQL datetime/datetime2 to CLR DateTime.
/// </summary>
[TypeOption(typeof(MsSqlConverters), "DateTime", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MsSqlDateTimeConverter()
    : DataTypeConverterBase(
        id: 5,
        name: "DateTime",
        sourceType: "datetime",
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
