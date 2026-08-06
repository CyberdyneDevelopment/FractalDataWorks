using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Converts PostgreSQL character to CLR String.
/// </summary>
[TypeOption(typeof(PostgreSqlConverters), "Char", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PostgreSqlCharConverter()
    : DataTypeConverterBase(
        id: 11,
        name: "Char",
        sourceType: "Char",
        targetClrType: typeof(string),
        dbType: DbType.AnsiStringFixedLength)
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
