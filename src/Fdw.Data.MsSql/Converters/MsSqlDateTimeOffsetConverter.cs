using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Converts SQL datetimeoffset to CLR DateTimeOffset.
/// </summary>
[TypeOption(typeof(MsSqlConverters), "DateTimeOffset", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MsSqlDateTimeOffsetConverter()
    : DataTypeConverterBase(
        id: 6,
        name: "DateTimeOffset",
        sourceType: "datetimeoffset",
        targetClrType: typeof(DateTimeOffset),
        dbType: DbType.DateTimeOffset)
{

    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        if (dbValue is DateTimeOffset dto)
        {
            return dto;
        }

        if (dbValue is DateTime dt)
        {
            return new DateTimeOffset(dt);
        }

        return new DateTimeOffset(Convert.ToDateTime(dbValue, CultureInfo.InvariantCulture));
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue;
    }
}
