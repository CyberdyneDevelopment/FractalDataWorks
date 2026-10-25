using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.RelativeTime;

/// <summary>
/// The start of the year, in the stated time zone.
/// </summary>
[TypeOption(typeof(TimeAnchors), "StartOfYear")]
[ExcludeFromCodeCoverage]
public sealed class StartOfYearTimeAnchor : CalendarTimeAnchorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StartOfYearTimeAnchor"/> class.
    /// </summary>
    public StartOfYearTimeAnchor()
        : base("StartOfYear", "The start of the year, in the stated time zone.")
    {
    }

    /// <inheritdoc />
    protected override DateTime StartOf(DateTime wall) => new DateTime(wall.Year, 1, 1);
}
