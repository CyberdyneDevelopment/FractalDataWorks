using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Scheduling.Commands;

/// <summary>ConfigurationCommands TypeOption for the Scheduler configuration domain (sched.Scheduler).</summary>
[TypeOption(typeof(ConfigurationCommands), "Scheduler")]
public sealed class SchedulerConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <inheritdoc/>
    public SchedulerConfigurationCommand() : base("Scheduler") { }
}
