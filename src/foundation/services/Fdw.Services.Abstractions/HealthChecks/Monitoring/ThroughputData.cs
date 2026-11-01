using System.Collections.Generic;

namespace Fdw.Services.Abstractions.Health.Monitoring;

/// <summary>
/// Represents throughput data for a service over a time window.
/// </summary>
public sealed class ThroughputData
{
    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the requests per second rate.
    /// </summary>
    public double RequestsPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the average latency in milliseconds.
    /// </summary>
    public double AvgLatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the 95th percentile latency in milliseconds.
    /// </summary>
    public double P95LatencyMs { get; set; }

    /// <summary>
    /// Gets or sets the error rate as a fraction (0.0 to 1.0).
    /// </summary>
    public double ErrorRate { get; set; }

    /// <summary>
    /// Gets or sets the time series data points.
    /// </summary>
    public IReadOnlyList<ThroughputDataPoint> DataPoints { get; set; } = [];
}
