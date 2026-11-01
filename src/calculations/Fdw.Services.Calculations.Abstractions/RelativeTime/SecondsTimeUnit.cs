using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Calculations.Abstractions.RelativeTime;

/// <summary>
/// Seconds of elapsed time.
/// </summary>
[TypeOption(typeof(TimeUnits), "Seconds")]
[ExcludeFromCodeCoverage]
public sealed class SecondsTimeUnit : ElapsedTimeUnitBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecondsTimeUnit"/> class.
    /// </summary>
    public SecondsTimeUnit()
        : base("Seconds", "seconds of elapsed time.", TimeSpan.TicksPerSecond)
    {
    }
}
