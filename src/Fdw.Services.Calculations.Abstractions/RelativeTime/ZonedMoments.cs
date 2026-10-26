using System;
using System.Linq;

namespace Fdw.Services.Calculations.Abstractions.RelativeTime;

/// <summary>
/// Moves between a moment in time and the wall-clock reading of a time zone, which is how a day, a week, a month and a year are
/// counted.
/// </summary>
/// <remarks>
/// A wall-clock reading that does not exist (the clock jumped forward) is moved forward by the hour the clock skipped. One that
/// happens twice (the clock fell back) is read as its first occurrence, the one with the larger offset.
/// </remarks>
internal static class ZonedMoments
{
    /// <summary>Gets the wall-clock reading of a moment in a zone.</summary>
    /// <param name="moment">The moment.</param>
    /// <param name="zone">The zone.</param>
    /// <returns>The reading, with an unspecified kind.</returns>
    public static DateTime WallOf(DateTimeOffset moment, TimeZoneInfo zone) =>
        DateTime.SpecifyKind(TimeZoneInfo.ConvertTime(moment, zone).DateTime, DateTimeKind.Unspecified);

    /// <summary>Gets the moment a wall-clock reading names in a zone.</summary>
    /// <param name="wall">The reading.</param>
    /// <param name="zone">The zone.</param>
    /// <returns>The moment.</returns>
    public static DateTimeOffset FromWall(DateTime wall, TimeZoneInfo zone)
    {
        var reading = DateTime.SpecifyKind(wall, DateTimeKind.Unspecified);
        if (zone.IsInvalidTime(reading))
        {
            reading = reading.AddHours(1);
        }

        var offset = zone.IsAmbiguousTime(reading) ? zone.GetAmbiguousTimeOffsets(reading).Max() : zone.GetUtcOffset(reading);
        return new DateTimeOffset(reading, offset);
    }
}
