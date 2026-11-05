using Fdw.Collections.Attributes;
using Fdw.Configuration;
using Fdw.Services.Configuration;

namespace Fdw.Services.Etl.Projects.Commands;

/// <summary>Configuration command for the pipe.ServerPolicyDefault domain table.</summary>
[TypeOption(typeof(ConfigurationCommands), "ServerPolicyDefault")]
public sealed class ServerPolicyDefaultConfigurationCommand : ConfigurationCommandBase<DomainConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ServerPolicyDefaultConfigurationCommand"/> class.</summary>
    public ServerPolicyDefaultConfigurationCommand() : base("ServerPolicyDefault") { }
}
