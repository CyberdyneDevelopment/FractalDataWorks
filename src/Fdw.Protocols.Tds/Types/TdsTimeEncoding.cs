using System;

namespace Fdw.Protocols.Tds.Types;

/// <summary>
/// The byte layouts of date, time, datetime2 and datetimeoffset values (MS-TDS 2.2.5.5.1.8).
/// </summary>
public static class TdsTimeEncoding
{
    private static readonly DateTime DateEpoch = new(1, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

    /// <summary>Gets how many bytes a time of the given fractional-second scale takes.</summary>
    /// <param name="scale">The scale, 0 to 7.</param>
    /// <returns>3, 4 or 5.</returns>
    public static int TimeLength(byte scale) => scale <= 2 ? 3 : scale <= 4 ? 4 : 5;

    /// <summary>Writes the time of day as units of 10^-scale seconds since midnight.</summary>
    /// <param name="writer">The payload being built.</param>
    /// <param name="timeOfDay">The time of day.</param>
    /// <param name="scale">The fractional-second scale.</param>
    public static void WriteTime(TdsPayloadWriter writer, TimeSpan timeOfDay, byte scale)
    {
        ArgumentNullException.ThrowIfNull(writer);
        var units = timeOfDay.Ticks / TicksPerUnit(scale);
        Span<byte> bytes = stackalloc byte[8];
        System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(bytes, units);
        writer.WriteBytes(bytes[..TimeLength(scale)]);
    }

    /// <summary>Writes a date as three bytes of days since 0001-01-01.</summary>
    /// <param name="writer">The payload being built.</param>
    /// <param name="date">The date; its time of day is ignored.</param>
    public static void WriteDate(TdsPayloadWriter writer, DateTime date)
    {
        ArgumentNullException.ThrowIfNull(writer);
        var days = (int)(date.Date - DateEpoch).TotalDays;
        writer.WriteByte((byte)(days & 0xFF));
        writer.WriteByte((byte)((days >> 8) & 0xFF));
        writer.WriteByte((byte)((days >> 16) & 0xFF));
    }

    /// <summary>Gets how many 100-nanosecond ticks one unit of the given scale holds.</summary>
    /// <param name="scale">The scale, 0 to 7.</param>
    /// <returns>The ticks per unit.</returns>
    public static long TicksPerUnit(byte scale)
    {
        long ticks = 1;
        for (var i = scale; i < 7; i++)
            ticks *= 10;

        return ticks;
    }
}
