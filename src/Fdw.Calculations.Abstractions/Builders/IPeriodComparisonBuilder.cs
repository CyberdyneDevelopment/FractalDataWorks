using System;
using Fdw.Calculations.Abstractions.PeriodComparisonTypeOptions;

namespace Fdw.Calculations.Builders;

/// <summary>
/// Fluent builder interface for configuring period-over-period comparison operations.
/// </summary>
/// <typeparam name="TOutput">The output type of the calculation.</typeparam>
public interface IPeriodComparisonBuilder<TOutput>
{
    /// <summary>
    /// Sets the type of period comparison (YoY, MoM, QoQ, etc.).
    /// </summary>
    /// <param name="comparisonType">The type of period comparison.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IPeriodComparisonBuilder<TOutput> WithComparisonType(IPeriodComparisonType comparisonType);

    /// <summary>
    /// Sets the current period for comparison.
    /// </summary>
    /// <param name="periodStart">The start date of the current period.</param>
    /// <param name="periodEnd">The end date of the current period.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IPeriodComparisonBuilder<TOutput> ForPeriod(DateTime periodStart, DateTime periodEnd);

    /// <summary>
    /// Sets the field/property name containing the date to use for period calculations.
    /// </summary>
    /// <param name="dateFieldName">The name of the date field.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IPeriodComparisonBuilder<TOutput> UsingDateField(string dateFieldName);

    /// <summary>
    /// Sets the metric field/property to compare across periods.
    /// </summary>
    /// <param name="metricFieldName">The name of the metric field.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IPeriodComparisonBuilder<TOutput> CompareMetric(string metricFieldName);

    /// <summary>
    /// Specifies whether to return the absolute difference or percentage change.
    /// </summary>
    /// <param name="asPercentage">True for percentage change, false for absolute difference.</param>
    /// <returns>The builder instance for method chaining.</returns>
    IPeriodComparisonBuilder<TOutput> AsPercentage(bool asPercentage = true);
}
