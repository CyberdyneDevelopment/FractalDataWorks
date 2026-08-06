using System;

namespace Fdw.Services.Abstractions.Health.Monitoring;

/// <summary>
/// Represents a single health check data point in a time series.
/// </summary>
public sealed class HealthCheckPoint
{
    /// <summary>
    /// Gets or sets the timestamp of this health check.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the health status at this point in time.
    /// </summary>
    public IHealthState Status { get; set; } = HealthStates.ByName("Healthy");

    /// <summary>
    /// Gets or sets the response time in milliseconds.
    /// </summary>
    public double ResponseTimeMs { get; set; }

    /// <summary>
    /// Gets or sets optional details about the health check.
    /// </summary>
    public string? Details { get; set; }
}
