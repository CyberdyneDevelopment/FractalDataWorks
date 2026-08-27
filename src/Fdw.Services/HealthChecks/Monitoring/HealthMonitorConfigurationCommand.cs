using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.HealthChecks.Monitoring;

/// <summary>
/// The configuration command for <see cref="HealthMonitorConfiguration"/> rows.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "HealthMonitor")]
public sealed class HealthMonitorConfigurationCommand : ConfigurationCommandBase<HealthMonitorConfiguration>
{
    /// <inheritdoc/>
    public HealthMonitorConfigurationCommand() : base("HealthMonitor") { }
}
