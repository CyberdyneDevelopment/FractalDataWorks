using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.RelativeTime;

/// <summary>
/// The start of the day, in the stated time zone.
/// </summary>
[TypeOption(typeof(TimeAnchors), "StartOfDay")]
[ExcludeFromCodeCoverage]
public sealed class StartOfDayTimeAnchor : CalendarTimeAnchorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StartOfDayTimeAnchor"/> class.
    /// </summary>
    public StartOfDayTimeAnchor()
        : base("StartOfDay", "The start of the day, in the stated time zone.")
    {
    }

    /// <inheritdoc />
    protected override DateTime StartOf(DateTime wall) => wall.Date;
}
