using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.RelativeTime;

/// <summary>
/// Calendar days: the same reading of the clock, day later or earlier in the stated time zone.
/// </summary>
[TypeOption(typeof(TimeUnits), "Days")]
[ExcludeFromCodeCoverage]
public sealed class DaysTimeUnit : CalendarTimeUnitBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DaysTimeUnit"/> class.
    /// </summary>
    public DaysTimeUnit()
        : base("Days", "Calendar days in the stated time zone.")
    {
    }

    /// <inheritdoc />
    protected override DateTime AddTo(DateTime wall, long amount) => wall.AddDays(amount);
}
