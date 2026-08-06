using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Converts SQL nvarchar/varchar to CLR String.
/// </summary>
[TypeOption(typeof(MsSqlConverters), "String", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MsSqlStringConverter()
    : DataTypeConverterBase(
        id: 3,
        name: "String",
        sourceType: "nvarchar",
        targetClrType: typeof(string),
        dbType: DbType.String)
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
