using System;

namespace Fdw.Services.Calculations.Abstractions.RelativeTime;

/// <summary>
/// A unit that can move a calendar date, which has no time of day and no time zone: days, weeks, months and years. Seconds, minutes
/// and hours are elapsed time and cannot move a date, so they do not implement this.
/// </summary>
public interface IDateUnit : ITimeUnit
{
    /// <summary>Adds a whole amount of this unit to a date.</summary>
    /// <param name="date">The date.</param>
    /// <param name="amount">The amount; negative counts back.</param>
    /// <returns>The date moved by the amount.</returns>
    DateOnly AddToDate(DateOnly date, long amount);
}
