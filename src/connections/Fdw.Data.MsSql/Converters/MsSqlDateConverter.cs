using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// Converts SQL date to CLR DateOnly.
/// </summary>
[TypeOption(typeof(MsSqlConverters), "Date", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MsSqlDateConverter()
    : DataTypeConverterBase(
        name: "Date",
        sourceType: "date",
        targetClrType: typeof(DateOnly),
        dbType: DbType.Date)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        if (dbValue is DateTime dt)
        {
            return DateOnly.FromDateTime(dt);
        }

        if (dbValue is DateOnly dateOnly)
        {
            return dateOnly;
        }

        return DateOnly.FromDateTime(Convert.ToDateTime(dbValue, System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue)
    {
        return clrValue switch
        {
            DateOnly dateOnly => dateOnly.ToDateTime(TimeOnly.MinValue),
            DateTimeOffset moment => moment.DateTime.Date,
            string text => DayOf(text),
            _ => clrValue,
        };
    }

    // A day written as text ("10/27/2026", "2026-10-27", with or without a time): the invariant culture reads the month first. Text that is not a date is returned as it came: ToDb has no
    // way to report a failure (no result), so the bulk plan, which has one, refuses the value with its own code (MSSQL-21030), naming the column and the kind of value.
    private static object DayOf(string text) =>
        DateTime.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var moment)
            ? moment.Date
            : text;
}
