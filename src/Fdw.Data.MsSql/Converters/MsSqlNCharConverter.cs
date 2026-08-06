using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Converts SQL nchar to CLR String.
/// </summary>
[TypeOption(typeof(MsSqlConverters), "NChar", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MsSqlNCharConverter()
    : DataTypeConverterBase(
        id: 21,
        name: "NChar",
        sourceType: "nchar",
        targetClrType: typeof(string),
        dbType: DbType.StringFixedLength)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        return dbValue.ToString();
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
