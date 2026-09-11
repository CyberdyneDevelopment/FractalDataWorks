using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Hosts.Configuration;

namespace Fdw.Services.Hosts.Commands;

/// <summary>Reads and writes the Host implementation rows.</summary>
[TypeOption(typeof(ConfigurationCommands), "HostImplementation")]
public sealed class HostImplementationConfigurationCommand : ConfigurationCommandBase<HostImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="HostImplementationConfigurationCommand"/> class.</summary>
    public HostImplementationConfigurationCommand()
        : base("HostImplementation")
    {
    }
}
