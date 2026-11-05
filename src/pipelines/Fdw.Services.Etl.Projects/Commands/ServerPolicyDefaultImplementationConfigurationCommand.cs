using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;

namespace Fdw.Services.Etl.Projects.Commands;

/// <summary>Configuration command for the pipe.ServerPolicyDefaultConfiguration implementation table.</summary>
[TypeOption(typeof(ConfigurationCommands), "ServerPolicyDefaultConfiguration")]
public sealed class ServerPolicyDefaultImplementationConfigurationCommand : ConfigurationCommandBase<ServerPolicyDefaultConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ServerPolicyDefaultImplementationConfigurationCommand"/> class.</summary>
    public ServerPolicyDefaultImplementationConfigurationCommand() : base("ServerPolicyDefaultConfiguration") { }
}
