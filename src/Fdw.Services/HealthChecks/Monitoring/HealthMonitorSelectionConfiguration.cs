using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.HealthChecks.Monitoring;

/// <summary>
/// Which health monitor row this host reports to.
/// </summary>
/// <remarks>
/// Monitor rows are shared; the selection is per host, which is why it is its own row on the
/// server tier rather than a column on the monitor. It was the HealthMonitor appsettings section.
/// </remarks>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "HealthMonitor", ServiceType = "HealthMonitorSelection")]
public sealed partial class HealthMonitorSelectionConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the identifier assigned by the store.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name of this configuration row.</summary>
    public string Name { get; set; } = string.Empty;

    string IGenericConfiguration.SectionName => "HealthMonitor";

    string IGenericConfiguration.ServiceType => "HealthMonitor";

    string? IGenericConfiguration.ServiceOptionType => "HealthMonitorSelection";

    /// <summary>Gets or sets the name of the monitor row this host reports to.</summary>
    public string MonitorName { get; set; } = string.Empty;
}
