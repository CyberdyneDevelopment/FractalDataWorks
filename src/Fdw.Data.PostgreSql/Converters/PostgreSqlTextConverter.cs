using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Converts PostgreSQL text to CLR String.
/// </summary>
[TypeOption(typeof(PostgreSqlConverters), "Text", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PostgreSqlTextConverter()
    : DataTypeConverterBase(
        id: 9,
        name: "Text",
        sourceType: "text",
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
