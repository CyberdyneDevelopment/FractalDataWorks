using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Converts PostgreSQL bytea to CLR byte array.
/// </summary>
[TypeOption(typeof(PostgreSqlConverters), "ByteArray", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PostgreSqlByteArrayConverter()
    : DataTypeConverterBase(
        id: 12,
        name: "ByteArray",
        sourceType: "bytea",
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

        if (dbValue is byte[] bytes)
        {
            return bytes;
        }

        throw new InvalidCastException($"Cannot convert {dbValue.GetType().Name} to byte[]");
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
