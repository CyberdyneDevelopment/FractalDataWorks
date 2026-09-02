using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.HealthChecks.Monitoring;

/// <summary>ConfigurationCommands TypeOption for the per-host health monitor selection.</summary>
[TypeOption(typeof(ConfigurationCommands), "HealthMonitorSelection")]
public sealed class HealthMonitorSelectionConfigurationCommand
    : ConfigurationCommandBase<HealthMonitorSelectionConfiguration>
{
    /// <inheritdoc/>
    public HealthMonitorSelectionConfigurationCommand() : base("HealthMonitorSelection") { }
}
