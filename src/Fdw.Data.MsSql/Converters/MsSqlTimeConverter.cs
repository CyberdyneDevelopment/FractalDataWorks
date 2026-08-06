using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Converts SQL time to CLR TimeOnly.
/// </summary>
[TypeOption(typeof(MsSqlConverters), "Time", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MsSqlTimeConverter()
    : DataTypeConverterBase(
        id: 13,
        name: "Time",
        sourceType: "time",
        targetClrType: typeof(TimeOnly),
        dbType: DbType.Time)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        if (dbValue is TimeSpan ts)
        {
            return TimeOnly.FromTimeSpan(ts);
        }

        if (dbValue is TimeOnly timeOnly)
        {
            return timeOnly;
        }

        if (dbValue is DateTime dt)
        {
            return TimeOnly.FromDateTime(dt);
        }

        return TimeOnly.Parse(dbValue.ToString()!, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        if (clrValue is TimeOnly timeOnly)
        {
            return timeOnly.ToTimeSpan();
        }

        return clrValue;
    }
}
