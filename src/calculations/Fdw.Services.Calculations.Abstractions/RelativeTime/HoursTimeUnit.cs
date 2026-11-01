using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.RelativeTime;

/// <summary>
/// Hours of elapsed time.
/// </summary>
[TypeOption(typeof(TimeUnits), "Hours")]
[ExcludeFromCodeCoverage]
public sealed class HoursTimeUnit : ElapsedTimeUnitBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HoursTimeUnit"/> class.
    /// </summary>
    public HoursTimeUnit()
        : base("Hours", "hours of elapsed time.", TimeSpan.TicksPerHour)
    {
    }
}
