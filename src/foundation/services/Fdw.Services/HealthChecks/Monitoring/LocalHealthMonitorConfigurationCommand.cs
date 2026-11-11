using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.HealthChecks.Monitoring;

/// <summary>
/// The configuration command for <see cref="LocalHealthMonitorConfiguration"/> rows.
/// </summary>
[TypeOption(typeof(ConfigurationCommands), "LocalHealthMonitorConfiguration")]
public sealed class LocalHealthMonitorConfigurationCommand
    : ConfigurationCommandBase<LocalHealthMonitorConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="LocalHealthMonitorConfigurationCommand"/> class.</summary>
    public LocalHealthMonitorConfigurationCommand() : base("LocalHealthMonitorConfiguration") { }
}
