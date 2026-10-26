using System;

namespace Fdw.Services.Calculations.Abstractions.RelativeTime;

/// <summary>
/// A unit of elapsed time: it moves the instant by a fixed length, whatever the clock on the wall says.
/// </summary>
public abstract class ElapsedTimeUnitBase : TimeUnitBase
{
    private readonly long _ticksPerUnit;

    /// <summary>
    /// Initializes a new instance of the <see cref="ElapsedTimeUnitBase"/> class.
    /// </summary>
    /// <param name="name">The unit's name.</param>
    /// <param name="summary">A one-sentence description.</param>
    /// <param name="ticksPerUnit">The length of one unit, in ticks.</param>
    protected ElapsedTimeUnitBase(string name, string summary, long ticksPerUnit)
        : base(name, summary)
    {
        _ticksPerUnit = ticksPerUnit;
    }

    /// <inheritdoc />
    public sealed override DateTimeOffset Add(DateTimeOffset moment, long amount, TimeZoneInfo zone) =>
        moment.Add(TimeSpan.FromTicks(checked(amount * _ticksPerUnit)));
}
