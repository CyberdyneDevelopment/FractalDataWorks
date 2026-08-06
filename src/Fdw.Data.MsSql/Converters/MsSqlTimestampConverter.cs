using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Converts SQL timestamp/rowversion to CLR Byte[].
/// </summary>
[TypeOption(typeof(MsSqlConverters), "Timestamp", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MsSqlTimestampConverter()
    : DataTypeConverterBase(
        id: 29,
        name: "Timestamp",
        sourceType: "timestamp",
        targetClrType: typeof(byte[]),
        dbType: DbType.Binary)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        return dbValue as byte[];
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
