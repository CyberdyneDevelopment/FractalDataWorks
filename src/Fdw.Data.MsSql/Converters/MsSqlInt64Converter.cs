using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Converts SQL bigint to CLR Int64.
/// </summary>
[TypeOption(typeof(MsSqlConverters), "Int64", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MsSqlInt64Converter()
    : DataTypeConverterBase(
        id: 2,
        name: "Int64",
        sourceType: "bigint",
        targetClrType: typeof(long),
        dbType: DbType.Int64)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        return Convert.ToInt64(dbValue, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
