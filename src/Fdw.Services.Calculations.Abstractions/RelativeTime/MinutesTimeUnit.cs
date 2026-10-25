using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.RelativeTime;

/// <summary>
/// Minutes of elapsed time.
/// </summary>
[TypeOption(typeof(TimeUnits), "Minutes")]
[ExcludeFromCodeCoverage]
public sealed class MinutesTimeUnit : ElapsedTimeUnitBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MinutesTimeUnit"/> class.
    /// </summary>
    public MinutesTimeUnit()
        : base("Minutes", "minutes of elapsed time.", TimeSpan.TicksPerMinute)
    {
    }
}
