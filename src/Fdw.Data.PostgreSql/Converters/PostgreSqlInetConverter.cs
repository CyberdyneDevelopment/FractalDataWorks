using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Converts PostgreSQL inet to CLR IPAddress.
/// </summary>
[TypeOption(typeof(PostgreSqlConverters), "Inet", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PostgreSqlInetConverter()
    : DataTypeConverterBase(
        id: 25,
        name: "Inet",
        sourceType: "inet",
        targetClrType: typeof(IPAddress),
        dbType: DbType.Object)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        if (dbValue is IPAddress ipAddress)
        {
            return ipAddress;
        }

        if (dbValue is string str)
        {
            return IPAddress.Parse(str);
        }

        throw new InvalidCastException($"Cannot convert {dbValue.GetType().Name} to IPAddress");
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
