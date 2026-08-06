using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// Converts PostgreSQL macaddr to CLR PhysicalAddress.
/// </summary>
[TypeOption(typeof(PostgreSqlConverters), "MacAddr", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PostgreSqlMacAddrConverter()
    : DataTypeConverterBase(
        id: 27,
        name: "MacAddr",
        sourceType: "macaddr",
        targetClrType: typeof(PhysicalAddress),
        dbType: DbType.Object)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        if (dbValue is PhysicalAddress physicalAddress)
        {
            return physicalAddress;
        }

        if (dbValue is string str)
        {
            return PhysicalAddress.Parse(str);
        }

        throw new InvalidCastException($"Cannot convert {dbValue.GetType().Name} to PhysicalAddress");
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
