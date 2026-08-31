using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Hosts.Commands;

/// <summary>Data commands for the logging domain table.</summary>
[TypeOption(typeof(ConfigurationCommands), "Host")]
public sealed class HostConfigurationCommand : ConfigurationCommandBase<HostConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="HostConfigurationCommand"/> class.</summary>
    public HostConfigurationCommand() : base("Host") { }
}
