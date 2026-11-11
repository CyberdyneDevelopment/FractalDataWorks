using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Workspaces.Commands;

/// <summary>ConfigurationCommands TypeOption for the Workspace configuration domain.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "Workspace")]
public sealed class WorkspaceConfigurationCommand : ConfigurationCommandBase<WorkspaceImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceConfigurationCommand"/> class.</summary>
    public WorkspaceConfigurationCommand() : base("Workspace") { }
}
