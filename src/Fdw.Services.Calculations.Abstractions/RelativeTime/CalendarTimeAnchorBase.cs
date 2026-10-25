using System;

namespace Fdw.Services.Calculations.Abstractions.RelativeTime;

/// <summary>
/// An anchor that cuts the wall-clock reading of a time zone back to the start of a calendar period.
/// </summary>
public abstract class CalendarTimeAnchorBase : TimeAnchorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarTimeAnchorBase"/> class.
    /// </summary>
    /// <param name="name">The anchor's name.</param>
    /// <param name="summary">A one-sentence description.</param>
    protected CalendarTimeAnchorBase(string name, string summary)
        : base(name, summary)
    {
    }

    /// <inheritdoc />
    public sealed override DateTimeOffset Apply(DateTimeOffset moment, TimeZoneInfo zone) =>
        ZonedMoments.FromWall(StartOf(ZonedMoments.WallOf(moment, zone)), zone);

    /// <summary>Gets the start of the period a wall-clock reading falls in.</summary>
    /// <param name="wall">The reading.</param>
    /// <returns>The reading at the start of the period.</returns>
    protected abstract DateTime StartOf(DateTime wall);
}
