using System;
using Fdw.Collections;

namespace Fdw.Services.Calculations.Abstractions.RelativeTime;

/// <summary>
/// The point of the calendar the current moment is cut back to: not at all, or the start of the day, week, month or year.
/// </summary>
public interface ITimeAnchor : ITypeOption<int, TimeAnchorBase>
{
    /// <summary>Gets a one-sentence description of the anchor.</summary>
    string Summary { get; }

    /// <summary>Cuts a moment back to this anchor.</summary>
    /// <param name="moment">The moment.</param>
    /// <param name="zone">The time zone the calendar is read in.</param>
    /// <returns>The anchored moment.</returns>
    DateTimeOffset Apply(DateTimeOffset moment, TimeZoneInfo zone);
}
