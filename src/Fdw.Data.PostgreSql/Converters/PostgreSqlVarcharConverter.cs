using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Converts PostgreSQL character varying to CLR String.
/// </summary>
[TypeOption(typeof(PostgreSqlConverters), "Varchar", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PostgreSqlVarcharConverter()
    : DataTypeConverterBase(
        id: 10,
        name: "Varchar",
        sourceType: "varchar",
        targetClrType: typeof(string),
        dbType: DbType.AnsiString)
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
