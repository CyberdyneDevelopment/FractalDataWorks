using System;

namespace Fdw.Services.Abstractions.Health.Monitoring;

/// <summary>
/// Represents a single throughput data point in a time series.
/// </summary>
// Why: pure data-transfer POCO, auto-properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ThroughputDataPoint
{
    /// <summary>
    /// Gets or sets the timestamp of this data point.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the number of requests during this interval.
    /// </summary>
    public long RequestCount { get; set; }

    /// <summary>
    /// Gets or sets the average latency in milliseconds during this interval.
    /// </summary>
    public double AverageLatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the number of errors during this interval.
    /// </summary>
    public int ErrorCount { get; set; }
}
