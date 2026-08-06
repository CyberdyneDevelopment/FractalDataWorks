using System;

namespace Fdw.Services.Abstractions.Health.Monitoring;

/// <summary>
/// Represents the health snapshot of a single service.
/// </summary>
public sealed class ServiceHealthSnapshot
{
    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current health status.
    /// </summary>
    public IHealthState Status { get; set; } = HealthStates.ByName("Healthy");

    /// <summary>
    /// Gets or sets the response time in milliseconds.
    /// </summary>
    public double ResponseTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of the last health check.
    /// </summary>
    public DateTimeOffset LastCheckAt { get; set; }

    /// <summary>
    /// Gets or sets the service uptime.
    /// </summary>
    public TimeSpan Uptime { get; set; }
}
