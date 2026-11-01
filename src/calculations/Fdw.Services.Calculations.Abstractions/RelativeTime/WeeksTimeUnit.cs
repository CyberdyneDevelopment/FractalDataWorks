using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.RelativeTime;

/// <summary>
/// Calendar weeks: the same reading of the clock, week later or earlier in the stated time zone.
/// </summary>
[TypeOption(typeof(TimeUnits), "Weeks")]
[ExcludeFromCodeCoverage]
public sealed class WeeksTimeUnit : CalendarTimeUnitBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WeeksTimeUnit"/> class.
    /// </summary>
    public WeeksTimeUnit()
        : base("Weeks", "Calendar weeks in the stated time zone.")
    {
    }

    /// <inheritdoc />
    protected override DateTime AddTo(DateTime wall, long amount) => wall.AddDays(checked(amount * 7));
}
