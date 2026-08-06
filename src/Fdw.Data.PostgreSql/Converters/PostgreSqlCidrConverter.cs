using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Converts PostgreSQL cidr to CLR String.
/// </summary>
[TypeOption(typeof(PostgreSqlConverters), "Cidr", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PostgreSqlCidrConverter()
    : DataTypeConverterBase(
        id: 26,
        name: "Cidr",
        sourceType: "cidr",
        targetClrType: typeof(string),
        dbType: DbType.Object)
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
