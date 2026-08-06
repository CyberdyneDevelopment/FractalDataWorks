using System;
using System.Collections;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Converts PostgreSQL bit varying to CLR BitArray.
/// </summary>
[TypeOption(typeof(PostgreSqlConverters), "BitVarying", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PostgreSqlBitVaryingConverter()
    : DataTypeConverterBase(
        id: 24,
        name: "BitVarying",
        sourceType: "bit varying",
        targetClrType: typeof(BitArray),
        dbType: DbType.Object)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        if (dbValue is BitArray bitArray)
        {
            return bitArray;
        }

        throw new InvalidCastException($"Cannot convert {dbValue.GetType().Name} to BitArray");
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
