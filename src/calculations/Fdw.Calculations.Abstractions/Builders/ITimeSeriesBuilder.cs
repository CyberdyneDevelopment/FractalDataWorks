using System;

namespace Fdw.Calculations.Builders;

/// <summary>
/// Fluent builder interface for configuring time-series operations.
/// </summary>
/// <typeparam name="TOutput">The output type of the calculation.</typeparam>
public interface ITimeSeriesBuilder<TOutput>
{
    /// <summary>
    /// Sets the window size for rolling calculations.
    /// </summary>
    /// <param name="windowSize">The number of periods to include in the rolling window.</param>
    /// <returns>The builder instance for method chaining.</returns>
    ITimeSeriesBuilder<TOutput> WithRollingWindow(int windowSize);

    /// <summary>
    /// Sets the date field to use for time-series ordering.
    /// </summary>
    /// <param name="dateFieldName">The name of the date field.</param>
    /// <returns>The builder instance for method chaining.</returns>
    ITimeSeriesBuilder<TOutput> OrderByDate(string dateFieldName);

    /// <summary>
    /// Sets the metric field to calculate rolling values for.
    /// </summary>
    /// <param name="metricFieldName">The name of the metric field.</param>
    /// <returns>The builder instance for method chaining.</returns>
    ITimeSeriesBuilder<TOutput> ForMetric(string metricFieldName);

    /// <summary>
    /// Specifies the calculation to perform on the rolling window (average, sum, etc.).
    /// </summary>
    /// <param name="calculation">The calculation type.</param>
    /// <returns>The builder instance for method chaining.</returns>
    ITimeSeriesBuilder<TOutput> Calculate(string calculation);
}
