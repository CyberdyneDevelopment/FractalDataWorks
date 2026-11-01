using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.RelativeTime;

/// <summary>
/// The start of the month, in the stated time zone.
/// </summary>
[TypeOption(typeof(TimeAnchors), "StartOfMonth")]
[ExcludeFromCodeCoverage]
public sealed class StartOfMonthTimeAnchor : CalendarTimeAnchorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StartOfMonthTimeAnchor"/> class.
    /// </summary>
    public StartOfMonthTimeAnchor()
        : base("StartOfMonth", "The start of the month, in the stated time zone.")
    {
    }

    /// <inheritdoc />
    protected override DateTime StartOf(DateTime wall) => new DateTime(wall.Year, wall.Month, 1);
}
