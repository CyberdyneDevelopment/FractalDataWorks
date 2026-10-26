using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.RelativeTime;

/// <summary>
/// The start of the week, which begins on Monday, in the stated time zone.
/// </summary>
[TypeOption(typeof(TimeAnchors), "StartOfWeek")]
[ExcludeFromCodeCoverage]
public sealed class StartOfWeekTimeAnchor : CalendarTimeAnchorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StartOfWeekTimeAnchor"/> class.
    /// </summary>
    public StartOfWeekTimeAnchor()
        : base("StartOfWeek", "The start of the week, which begins on Monday, in the stated time zone.")
    {
    }

    /// <inheritdoc />
    protected override DateTime StartOf(DateTime wall) => wall.Date.AddDays(-(((int)wall.DayOfWeek + 6) % 7));
}
