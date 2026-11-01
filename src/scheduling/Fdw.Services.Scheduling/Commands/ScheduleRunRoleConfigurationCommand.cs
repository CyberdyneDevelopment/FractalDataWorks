using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Scheduling.Abstractions.Configuration;

namespace Fdw.Services.Scheduling.Commands;

/// <summary>Reads and writes the roles a schedule's runs act with (sched.ScheduleRunRole).</summary>
/// <remarks>
/// Why: the configuration cascade resolves the child command for a <see cref="ScheduleRunRoleConfiguration"/> item through
/// <c>ConfigurationCommands.All()</c>; without this TypeOption a schedule that records roles fails to load or save with
/// NoChildCommandForType.
/// </remarks>
[TypeOption(typeof(ConfigurationCommands), "ScheduleRunRole")]
public sealed class ScheduleRunRoleConfigurationCommand : ConfigurationCommandBase<ScheduleRunRoleConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ScheduleRunRoleConfigurationCommand"/> class.</summary>
    public ScheduleRunRoleConfigurationCommand() : base("ScheduleRunRole")
    {
    }
}
