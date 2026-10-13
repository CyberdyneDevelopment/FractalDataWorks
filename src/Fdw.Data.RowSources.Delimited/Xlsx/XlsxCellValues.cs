using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>Turns a cell's stored text into the value it holds, by the cell's type and number format.</summary>
internal static class XlsxCellValues
{
    // Excel's 1900 calendar counts a day 1900-02-29 that never existed, so a serial of 61 or more counts from 1899-12-30 and
    // a serial below 60 from 1899-12-31. The 1904 calendar has no such day.
    private static readonly DateTime Epoch1900 = new(1899, 12, 30, 0, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateTime EpochBefore1900Leap = new(1899, 12, 31, 0, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateTime Epoch1904 = new(1904, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
    private const decimal NonexistentLeapDay = 60m;
    private const decimal Last1900Serial = 2958465m;
    private const decimal Last1904Serial = 2957003m;

    /// <summary>Converts a cell.</summary>
    /// <param name="type">The cell's t attribute; null is a number.</param>
    /// <param name="text">The stored text: a v element's, an inline string's, or null for a cell with neither.</param>
    /// <param name="kind">What the cell's number format makes a number.</param>
    /// <param name="date1904">Whether the workbook counts dates from 1904.</param>
    /// <param name="shared">The shared string table.</param>
    /// <returns>The value; null for an empty cell.</returns>
    /// <exception cref="InvalidDataException">The text is not what the cell's type says.</exception>
    internal static object? Convert(string? type, string? text, XlsxValueKind kind, bool date1904, IReadOnlyList<string> shared)
    {
        if (text is null)
        {
            return null;
        }

        return type switch
        {
            "s" => Shared(text, shared),
            "inlineStr" or "str" or "e" => text,
            "b" => Flag(text),
            "d" => Iso(text),
            null or "n" => Numeric(text, kind, date1904),
            _ => throw new InvalidDataException($"A cell has the type '{type}', which is not a worksheet cell type."),
        };
    }

    private static string Shared(string text, IReadOnlyList<string> shared)
    {
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var index) || index >= shared.Count)
        {
            throw new InvalidDataException($"A cell points at shared string '{text}', which the workbook does not have.");
        }

        return shared[index];
    }

    private static bool Flag(string text)
        => string.Equals(text, "1", StringComparison.Ordinal)
            ? true
            : string.Equals(text, "0", StringComparison.Ordinal)
                ? false
                : throw new InvalidDataException($"'{text}' is not a boolean cell value (0 or 1).");

    private static DateTime Iso(string text)
        => DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var moment)
            ? moment
            : throw new InvalidDataException($"'{text}' is not an ISO 8601 date.");

    private static object? Numeric(string text, XlsxValueKind kind, bool date1904)
    {
        if (text.Length == 0)
        {
            return null;
        }

        if (kind == XlsxValueKind.Number)
        {
            return Plain(text);
        }

        if (!decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial) || serial < 0 || serial > (date1904 ? Last1904Serial : Last1900Serial))
        {
            throw new InvalidDataException($"'{text}' is not a date or time serial number.");
        }

        return kind == XlsxValueKind.Time ? Elapsed(serial) : Moment(serial, date1904);
    }

    // A whole number stays whole, so an id of 12 is a long and not 12.0; anything with a fraction or an exponent is a decimal,
    // and only a value beyond decimal's range falls to a double.
    private static object Plain(string text)
    {
        if (long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var whole))
        {
            return whole;
        }

        if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            return number;
        }

        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var big)
            ? big
            : throw new InvalidDataException($"'{text}' is not a number.");
    }

    // Why to the millisecond: a serial is a double of days, and at today's dates its last digits are noise well below a
    // millisecond, which is also the resolution Excel itself works at. Reading the ticks would hand back 06:30:00.0000003.
    private static long Milliseconds(decimal serial) => (long)Math.Round((double)serial * 86_400_000d);

    private static DateTime Moment(decimal serial, bool date1904)
    {
        if (date1904)
        {
            return Epoch1904.AddTicks(Milliseconds(serial) * TimeSpan.TicksPerMillisecond);
        }

        if (serial == NonexistentLeapDay)
        {
            throw new InvalidDataException("A date cell holds serial 60, which is Excel's 1900-02-29, a day that does not exist.");
        }

        return (serial < NonexistentLeapDay ? EpochBefore1900Leap : Epoch1900).AddTicks(Milliseconds(serial) * TimeSpan.TicksPerMillisecond);
    }

    // A time of day is a fraction of a day; an elapsed time over a day is a span and not a time of day.
    private static object Elapsed(decimal serial)
        => serial < 1m
            ? TimeOnly.FromTimeSpan(TimeSpan.FromMilliseconds(Milliseconds(serial)))
            : TimeSpan.FromMilliseconds(Milliseconds(serial));
}
