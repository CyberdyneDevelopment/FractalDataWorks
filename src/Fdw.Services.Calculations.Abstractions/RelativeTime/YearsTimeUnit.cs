using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.RelativeTime;

/// <summary>
/// Calendar years: the same reading of the clock, year later or earlier in the stated time zone.
/// </summary>
[TypeOption(typeof(TimeUnits), "Years")]
[ExcludeFromCodeCoverage]
public sealed class YearsTimeUnit : CalendarTimeUnitBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YearsTimeUnit"/> class.
    /// </summary>
    public YearsTimeUnit()
        : base("Years", "Calendar years in the stated time zone.")
    {
    }

    /// <inheritdoc />
    protected override DateTime AddTo(DateTime wall, long amount) => wall.AddYears(checked((int)amount));
}
