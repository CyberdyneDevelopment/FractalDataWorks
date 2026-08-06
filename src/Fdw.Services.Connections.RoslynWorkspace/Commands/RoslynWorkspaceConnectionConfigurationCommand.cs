using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Connections.RoslynWorkspace.Commands;

/// <summary>ConfigurationCommands TypeOption for the RoslynWorkspaceConnection configuration domain.</summary>
[TypeOption(typeof(ConfigurationCommands), "RoslynWorkspaceConnection")]
public sealed class RoslynWorkspaceConnectionConfigurationCommand : ConfigurationCommandBase<RoslynWorkspaceConnectionConfiguration>
{
    /// <summary>Initializes the command with table name 'RoslynWorkspaceConnection'.</summary>
    public RoslynWorkspaceConnectionConfigurationCommand() : base("RoslynWorkspaceConnection") { }
}
