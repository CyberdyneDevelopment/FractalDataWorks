using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Scheduling.Commands;

/// <summary>ConfigurationCommands TypeOption for the Scheduler configuration domain (sched.Scheduler).</summary>
[TypeOption(typeof(ConfigurationCommands), "Scheduler")]
public sealed class SchedulerConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="SchedulerConfigurationCommand"/> class.</summary>
    public SchedulerConfigurationCommand() : base("Scheduler") { }
}
