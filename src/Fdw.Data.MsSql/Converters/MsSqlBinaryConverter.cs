using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Converts SQL binary to CLR Byte[].
/// </summary>
[TypeOption(typeof(MsSqlConverters), "Binary", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MsSqlBinaryConverter()
    : DataTypeConverterBase(
        id: 24,
        name: "Binary",
        sourceType: "binary",
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
