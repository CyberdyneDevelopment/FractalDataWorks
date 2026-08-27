using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.HealthChecks.Monitoring;

/// <summary>
/// The configuration command for <see cref="LocalHealthMonitorConfiguration"/> rows.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "LocalHealthMonitor")]
public sealed class LocalHealthMonitorConfigurationCommand
    : ConfigurationCommandBase<LocalHealthMonitorConfiguration>
{
    /// <inheritdoc/>
    public LocalHealthMonitorConfigurationCommand() : base("LocalHealthMonitor") { }
}
