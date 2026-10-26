using System;

namespace Fdw.Services.Calculations.Abstractions.RelativeTime;

/// <summary>
/// A unit of calendar time: it moves the wall-clock reading of a time zone, so a day is a calendar day and a month or a year
/// keeps its place in the calendar, across a change of the clocks.
/// </summary>
public abstract class CalendarTimeUnitBase : TimeUnitBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarTimeUnitBase"/> class.
    /// </summary>
    /// <param name="name">The unit's name.</param>
    /// <param name="summary">A one-sentence description.</param>
    protected CalendarTimeUnitBase(string name, string summary)
        : base(name, summary)
    {
    }

    /// <inheritdoc />
    public sealed override DateTimeOffset Add(DateTimeOffset moment, long amount, TimeZoneInfo zone) =>
        ZonedMoments.FromWall(AddTo(ZonedMoments.WallOf(moment, zone), amount), zone);

    /// <summary>Adds the amount to a wall-clock reading.</summary>
    /// <param name="wall">The reading.</param>
    /// <param name="amount">The amount; negative counts back.</param>
    /// <returns>The reading moved by the amount.</returns>
    protected abstract DateTime AddTo(DateTime wall, long amount);
}
