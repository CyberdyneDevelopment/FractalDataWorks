using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.RelativeTime;

/// <summary>
/// Calendar months: the same reading of the clock, month later or earlier in the stated time zone.
/// </summary>
[TypeOption(typeof(TimeUnits), "Months")]
[ExcludeFromCodeCoverage]
public sealed class MonthsTimeUnit : CalendarTimeUnitBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MonthsTimeUnit"/> class.
    /// </summary>
    public MonthsTimeUnit()
        : base("Months", "Calendar months in the stated time zone.")
    {
    }

    /// <inheritdoc />
    protected override DateTime AddTo(DateTime wall, long amount) => wall.AddMonths(checked((int)amount));
}
