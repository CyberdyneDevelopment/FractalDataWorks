using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Hosting.Commands;

/// <summary>Data commands for the logging domain table.</summary>
[TypeOption(typeof(ConfigurationCommands), "Hosting")]
public sealed class HostingConfigurationCommand : ConfigurationCommandBase<HostingConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="HostingConfigurationCommand"/> class.</summary>
    public HostingConfigurationCommand() : base("Hosting") { }
}
