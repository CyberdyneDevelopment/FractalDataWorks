using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Converts SQL bit to CLR Boolean.
/// </summary>
[TypeOption(typeof(MsSqlConverters), "Boolean", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MsSqlBooleanConverter()
    : DataTypeConverterBase(
        id: 4,
        name: "Boolean",
        sourceType: "bit",
        targetClrType: typeof(bool),
        dbType: DbType.Boolean)
{

    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        return Convert.ToBoolean(dbValue, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
