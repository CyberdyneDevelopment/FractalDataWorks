using System;
using System.Collections.Generic;

namespace Fdw.Services.Abstractions.Health.Monitoring;

/// <summary>
/// Represents the health snapshot of the entire system.
/// </summary>
public sealed class SystemHealthSnapshot
{
    /// <summary>
    /// Gets or sets the overall system health status.
    /// </summary>
    public IHealthState OverallStatus { get; set; } = HealthStates.ByName("Healthy");

    /// <summary>
    /// Gets or sets the health snapshots for all monitored services.
    /// </summary>
    public IReadOnlyList<ServiceHealthSnapshot> Services { get; set; } = [];

    /// <summary>
    /// Gets or sets the timestamp when this snapshot was taken.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}
