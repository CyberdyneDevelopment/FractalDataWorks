using System;
using Fdw.Collections;

namespace Fdw.Calculations.Abstractions.PeriodComparisonTypeOptions;

/// <summary>
/// Interface for period comparison types used in time-series analysis.
/// Extends ITypeOption to enable TypeCollection discovery.
/// </summary>
public interface IPeriodComparisonType : ITypeOption<int, PeriodComparisonTypeBase>
{
    /// <summary>
    /// Gets a value indicating whether this is a cumulative comparison (e.g., Year-to-Date).
    /// </summary>
    bool IsCumulative { get; }

    /// <summary>
    /// Gets the time span offset to the comparison period, or null if not applicable.
    /// </summary>
    /// <remarks>
    /// For example, YearOverYear returns a negative 1 year offset.
    /// Cumulative types return null since they don't compare to a prior period.
    /// </remarks>
    TimeSpan? ComparisonOffset { get; }

    /// <summary>
    /// Calculates the comparison date for a given reference date.
    /// </summary>
    /// <param name="referenceDate">The reference date to compare from.</param>
    /// <returns>The comparison date, or null if this is a cumulative type.</returns>
    DateTimeOffset? GetComparisonDate(DateTimeOffset referenceDate);

    /// <summary>
    /// Gets the start date for cumulative calculations.
    /// </summary>
    /// <param name="referenceDate">The reference date.</param>
    /// <returns>The start of the cumulative period, or null if not cumulative.</returns>
    DateTimeOffset? GetCumulativeStartDate(DateTimeOffset referenceDate);
}
