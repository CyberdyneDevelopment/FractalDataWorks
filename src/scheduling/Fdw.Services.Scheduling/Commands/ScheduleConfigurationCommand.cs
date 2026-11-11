using Fdw.Configuration;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Scheduling.Abstractions.Configuration;

namespace Fdw.Services.Scheduling.Commands;

/// <summary>ConfigurationCommands TypeOption for the Schedule configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "Schedule")]
public sealed class ScheduleConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ScheduleConfigurationCommand"/> class.</summary>
    public ScheduleConfigurationCommand() : base("Schedule") { }
}
