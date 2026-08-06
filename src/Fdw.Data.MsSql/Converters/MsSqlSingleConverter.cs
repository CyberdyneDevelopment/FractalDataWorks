using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Converts SQL real to CLR Single (float).
/// </summary>
[TypeOption(typeof(MsSqlConverters), "Single", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MsSqlSingleConverter()
    : DataTypeConverterBase(
        id: 17,
        name: "Single",
        sourceType: "real",
        targetClrType: typeof(float),
        dbType: DbType.Single)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        return Convert.ToSingle(dbValue, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
