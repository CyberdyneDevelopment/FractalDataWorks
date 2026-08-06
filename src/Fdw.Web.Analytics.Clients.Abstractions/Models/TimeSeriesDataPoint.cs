using System;

namespace Fdw.Web.Analytics.Clients.Models;

/// <summary>
/// Represents a single data point in a time series of execution metrics.
/// </summary>
// Why: pure data-transfer POCO, auto-properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class TimeSeriesDataPoint
{
    /// <summary>
    /// Gets or sets the timestamp of this data point.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the number of executions during this interval.
    /// </summary>
    public long ExecutionCount { get; set; }

    /// <summary>
    /// Gets or sets the average execution duration in milliseconds during this interval.
    /// </summary>
    public double AverageDurationMs { get; set; }

    /// <summary>
    /// Gets or sets the number of errors during this interval.
    /// </summary>
    public int ErrorCount { get; set; }
}
