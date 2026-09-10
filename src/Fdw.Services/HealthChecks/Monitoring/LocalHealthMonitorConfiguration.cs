using System.ComponentModel.DataAnnotations.Schema;
using System;
using Fdw.Data;
using Fdw.Services.Abstractions.Health.Monitoring;

namespace Fdw.Services.HealthChecks.Monitoring;

/// <summary>
/// The local health monitor's own configuration.
/// </summary>
[GenerateMapper]
public sealed partial class LocalHealthMonitorConfiguration : ILocalHealthMonitorConfiguration
{
    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

    /// <inheritdoc/>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Implementation { get; set; } = string.Empty;

    /// <inheritdoc/>
    public Guid Id { get; set; }

    /// <inheritdoc/>
    // Why it maps with no column of its own: the name is the domain's, and there is one
    // name for a configured member. The domain provider joins the domain row to this one,
    // and the join's result set is what carries it in.
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the owning health monitor's durable id.</summary>
    public Guid HealthMonitorId { get; set; }

    /// <summary>Gets or sets the interval between health checks, in seconds.</summary>
    public int CheckIntervalSeconds { get; set; }

    /// <summary>Gets or sets how long history is retained, in minutes.</summary>
    public int HistoryRetentionMinutes { get; set; }

    /// <summary>Gets or sets the throughput window, in seconds.</summary>
    public int ThroughputWindowSeconds { get; set; }
}
