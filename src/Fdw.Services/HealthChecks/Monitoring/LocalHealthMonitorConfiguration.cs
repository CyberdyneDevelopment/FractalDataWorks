using System;
using Fdw.Services.Abstractions.Health.Monitoring;

namespace Fdw.Services.HealthChecks.Monitoring;

/// <summary>
/// The local health monitor's own configuration.
/// </summary>
public sealed partial class LocalHealthMonitorConfiguration : IHealthMonitorImplementationConfiguration
{
    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <inheritdoc/>
    public string Name { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string SectionName => "HealthMonitors";

    /// <inheritdoc/>
    public string ServiceType => "HealthMonitor";

    /// <inheritdoc/>
    public string? ServiceOptionType { get; set; }

    /// <summary>Gets or sets the owning health monitor's durable id.</summary>
    public Guid HealthMonitorId { get; set; }

    /// <summary>Gets or sets the interval between health checks, in seconds.</summary>
    public int CheckIntervalSeconds { get; set; }

    /// <summary>Gets or sets how long history is retained, in minutes.</summary>
    public int HistoryRetentionMinutes { get; set; }

    /// <summary>Gets or sets the throughput window, in seconds.</summary>
    public int ThroughputWindowSeconds { get; set; }
}
