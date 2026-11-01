using System;
using Fdw.Collections;

namespace Fdw.Services.Calculations.Abstractions.RelativeTime;

/// <summary>
/// Base class for the units an amount of time is counted in (CRTP pattern).
/// </summary>
public abstract class TimeUnitBase : TypeOptionBase<TimeUnitBase>, ITimeUnit
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimeUnitBase"/> class.
    /// </summary>
    /// <param name="name">The unit's name, as a node states it.</param>
    /// <param name="summary">A one-sentence description.</param>
    protected TimeUnitBase(string name, string summary)
        : base(name)
    {
        Summary = summary;
    }

    /// <inheritdoc />
    public string Summary { get; }

    /// <inheritdoc />
    public abstract DateTimeOffset Add(DateTimeOffset moment, long amount, TimeZoneInfo zone);
}
