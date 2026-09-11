using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Scheduling.Abstractions.Configuration;

namespace Fdw.Services.Scheduling.Commands;

/// <summary>Reads and writes the Schedule implementation rows.</summary>
[TypeOption(typeof(ConfigurationCommands), "ScheduleImplementation")]
public sealed class ScheduleImplementationConfigurationCommand : ConfigurationCommandBase<ScheduleImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ScheduleImplementationConfigurationCommand"/> class.</summary>
    public ScheduleImplementationConfigurationCommand()
        : base("ScheduleImplementation")
    {
    }
}
