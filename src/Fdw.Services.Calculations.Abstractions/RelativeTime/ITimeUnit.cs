using System;
using Fdw.Collections;

namespace Fdw.Services.Calculations.Abstractions.RelativeTime;

/// <summary>
/// A unit an amount of time is counted in: seconds, minutes, hours, days, weeks, months or years.
/// </summary>
public interface ITimeUnit : ITypeOption<int, TimeUnitBase>
{
    /// <summary>Gets a one-sentence description of the unit.</summary>
    string Summary { get; }

    /// <summary>Adds a whole amount of this unit to a moment.</summary>
    /// <param name="moment">The moment.</param>
    /// <param name="amount">The amount; negative counts back.</param>
    /// <param name="zone">The time zone the calendar units are counted in.</param>
    /// <returns>The moment moved by the amount.</returns>
    DateTimeOffset Add(DateTimeOffset moment, long amount, TimeZoneInfo zone);
}
