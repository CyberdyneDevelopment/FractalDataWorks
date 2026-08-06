using System;
using System.Collections.Generic;

namespace Fdw.Web.Analytics.Clients.Models;

/// <summary>
/// Represents the response containing analytics data including summary, breakdown, and time series.
/// </summary>
public sealed class AnalyticsResponse
{
    /// <summary>
    /// Gets or sets the analytics summary for the requested period.
    /// </summary>
    public AnalyticsSummary Summary { get; set; } = new();

    /// <summary>
    /// Gets or sets the statistics broken down by calculation type.
    /// </summary>
    public IReadOnlyList<CalculationTypeStats> ByCalculationType { get; set; } = Array.Empty<CalculationTypeStats>();

    /// <summary>
    /// Gets or sets the time series data points for the requested period.
    /// </summary>
    public IReadOnlyList<TimeSeriesDataPoint> TimeSeries { get; set; } = Array.Empty<TimeSeriesDataPoint>();

    /// <summary>
    /// Gets or sets the top calculations by usage.
    /// </summary>
    public IReadOnlyList<CalculationTypeStats> TopCalculations { get; set; } = Array.Empty<CalculationTypeStats>();
}
