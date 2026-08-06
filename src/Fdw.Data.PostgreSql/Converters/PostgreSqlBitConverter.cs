using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Converts PostgreSQL bit(1) to CLR Boolean.
/// </summary>
[TypeOption(typeof(PostgreSqlConverters), "Bit", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PostgreSqlBitConverter()
    : DataTypeConverterBase(
        id: 23,
        name: "Bit",
        sourceType: "bit",
        targetClrType: typeof(bool),
        dbType: DbType.Boolean)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        return Convert.ToBoolean(dbValue, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
