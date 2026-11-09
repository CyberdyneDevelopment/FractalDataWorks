using System;
using System.Globalization;

namespace Fdw.Data.MsSql;

/// <summary>
/// Fits a value to the type of the <see cref="System.Data.DataTable"/> column that takes it. A column refuses a value of any other type ("Expected type is DateOnly"), and the
/// same day reaches a bulk copy as a <see cref="DateOnly"/> (from a typed entity), a <see cref="DateTime"/> or <see cref="DateTimeOffset"/> (from a mapper) or as text. Only a
/// <see cref="DateOnly"/> or <see cref="DateTime"/> column is fitted; a value for any other column is left exactly as it is, so a genuine mismatch there still fails where it is written.
/// </summary>
internal static class MsSqlBulkCopyColumnValue
{
    /// <summary>Fits the value to the column's type.</summary>
    /// <param name="columnType">The type of the DataTable column.</param>
    /// <param name="value">The value to write; never null (a null is written as DBNull by the caller).</param>
    /// <param name="fitted">The value in the column's type, or the value unchanged for a column that is not a date column.</param>
    /// <returns>False when the column takes a date and the value is not one (text that is not a date, or a value of another type).</returns>
    internal static bool TryFit(Type columnType, object value, out object fitted)
    {
        fitted = value;
        if (columnType == typeof(DateOnly))
        {
            var day = DayOf(value);
            if (day is null)
            {
                return false;
            }

            fitted = day.Value;
            return true;
        }

        if (columnType == typeof(DateTime))
        {
            var moment = MomentOf(value);
            if (moment is null)
            {
                return false;
            }

            fitted = moment.Value;
            return true;
        }

        return true;
    }

    // The day a value stands for, or null when it is not a date: text reads month first under the invariant culture, as the Date converter's ToDb does.
    private static DateOnly? DayOf(object value) => value switch
    {
        DateOnly day => day,
        _ => MomentOf(value) is { } moment ? DateOnly.FromDateTime(moment) : null,
    };

    // The date-time a value stands for, or null when it is not a date.
    private static DateTime? MomentOf(object value) => value switch
    {
        DateTime moment => moment,
        DateTimeOffset moment => moment.DateTime,
        DateOnly day => day.ToDateTime(TimeOnly.MinValue),
        string text when DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) => parsed,
        _ => null,
    };
}
