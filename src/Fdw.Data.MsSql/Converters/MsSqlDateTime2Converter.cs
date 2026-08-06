using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Converts SQL datetime2 to CLR DateTime.
/// </summary>
[TypeOption(typeof(MsSqlConverters), "DateTime2", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MsSqlDateTime2Converter()
    : DataTypeConverterBase(
        id: 11,
        name: "DateTime2",
        sourceType: "datetime2",
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
