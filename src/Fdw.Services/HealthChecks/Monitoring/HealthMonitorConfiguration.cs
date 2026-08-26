using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Services.Abstractions.Health.Monitoring;

namespace Fdw.Services.HealthChecks.Monitoring;

/// <summary>
/// Configuration for a health monitor service instance — a <c>settings.HealthMonitor</c> row in
/// ConfigurationDb.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ServiceOptionType"/> selects the registered option ("Local" or "HttpClient") — the
/// domain provider dispatches to that option's factory. Which ROW a host uses is that host's
/// <c>HealthMonitor:Name</c> selector knob (see <c>HealthMonitorSelectionOptions</c>) — rows are
/// shared in ConfigurationDb; the selection is per host.
/// </para>
/// <para>
/// No property carries a value default (NO FALLBACKS) — the seeded row supplies every value.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[ManagedConfiguration(ServiceCategory = "HealthMonitor",
    ServiceType = "HealthMonitor",
    DisplayName = "Health Monitor",
    Description = "Configuration for the health monitoring service including check intervals and retention.")]
public sealed partial class HealthMonitorConfiguration : IHealthMonitorConfiguration
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

    /// <summary>
    /// Gets or sets the interval between health checks in seconds.
    /// </summary>
    public int CheckIntervalSeconds { get; set; }

    /// <summary>
    /// Gets or sets the history retention period in minutes.
    /// </summary>
    public int HistoryRetentionMinutes { get; set; }

    /// <summary>
    /// Gets or sets the throughput window size in seconds.
    /// </summary>
    public int ThroughputWindowSeconds { get; set; }
}
