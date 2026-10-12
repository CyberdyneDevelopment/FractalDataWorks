using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.HealthChecks.Monitoring;

/// <summary>
/// The configuration command for <c>hlth.HealthMonitor</c> rows.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "HealthMonitor")]
public sealed class HealthMonitorConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <inheritdoc/>
    public HealthMonitorConfigurationCommand() : base("HealthMonitor") { }
}
