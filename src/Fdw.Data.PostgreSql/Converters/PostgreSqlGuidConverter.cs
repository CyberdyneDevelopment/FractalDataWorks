using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Converts PostgreSQL uuid to CLR Guid.
/// </summary>
[TypeOption(typeof(PostgreSqlConverters), "Guid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PostgreSqlGuidConverter()
    : DataTypeConverterBase(
        id: 13,
        name: "Guid",
        sourceType: "uuid",
        targetClrType: typeof(Guid),
        dbType: DbType.Guid)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        if (dbValue is Guid guid)
        {
            return guid;
        }

        if (dbValue is string str)
        {
            return Guid.Parse(str);
        }

        if (dbValue is byte[] bytes)
        {
            return new Guid(bytes);
        }

        throw new InvalidCastException($"Cannot convert {dbValue.GetType().Name} to Guid");
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
