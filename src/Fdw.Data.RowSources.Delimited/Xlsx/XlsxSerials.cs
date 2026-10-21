using System;
using System.Globalization;
using System.IO;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>Reads Excel's serial numbers: a date or a time of day is a count of days, with a fraction for the time.</summary>
internal static class XlsxSerials
{
    // Excel's 1900 calendar counts a day 1900-02-29 that never existed, so a serial of 61 or more counts from 1899-12-30 and
    // a serial below 60 from 1899-12-31. The 1904 calendar has no such day.
    private static readonly DateTime Epoch1900 = new(1899, 12, 30, 0, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateTime EpochBefore1900Leap = new(1899, 12, 31, 0, 0, 0, DateTimeKind.Unspecified);
    private static readonly DateTime Epoch1904 = new(1904, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
    private const decimal NonexistentLeapDay = 60m;
    private const decimal Last1900Serial = 2958465m;
    private const decimal Last1904Serial = 2957003m;

    /// <summary>Parses a serial number, refusing one outside the calendar's range.</summary>
    /// <param name="text">The stored text.</param>
    /// <param name="date1904">Whether the workbook counts dates from 1904.</param>
    /// <returns>The serial.</returns>
    /// <exception cref="InvalidDataException">The text is not a serial number the calendar can hold.</exception>
    internal static decimal Parse(string text, bool date1904)
        => decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial)
           && serial >= 0
           && serial <= (date1904 ? Last1904Serial : Last1900Serial)
            ? serial
            : throw new InvalidDataException($"'{text}' is not a date or time serial number.");

    // Why to the millisecond: a serial is a double of days, and at today's dates its last digits are noise well below a
    // millisecond, which is also the resolution Excel itself works at. Reading the ticks would hand back 06:30:00.0000003.
    private static long Milliseconds(decimal serial) => (long)Math.Round((double)serial * 86_400_000d);

    /// <summary>Turns a serial into the date and time it counts to.</summary>
    /// <param name="serial">The serial.</param>
    /// <param name="date1904">Whether the workbook counts dates from 1904.</param>
    /// <returns>The date and time.</returns>
    /// <exception cref="InvalidDataException">The serial is the 1900 calendar's nonexistent 1900-02-29.</exception>
    internal static DateTime Moment(decimal serial, bool date1904)
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

    /// <summary>Turns a serial into a time of day, or a span when it is a day or more.</summary>
    /// <param name="serial">The serial.</param>
    /// <returns>A <see cref="TimeOnly"/> below one day; a <see cref="TimeSpan"/> otherwise.</returns>
    // A time of day is a fraction of a day; an elapsed time over a day is a span and not a time of day.
    internal static object Elapsed(decimal serial)
        => serial < 1m
            ? TimeOnly.FromTimeSpan(TimeSpan.FromMilliseconds(Milliseconds(serial)))
            : TimeSpan.FromMilliseconds(Milliseconds(serial));
}
