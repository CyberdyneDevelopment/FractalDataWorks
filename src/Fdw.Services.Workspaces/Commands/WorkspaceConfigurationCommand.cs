using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Workspaces.Commands;

/// <summary>ConfigurationCommands TypeOption for the Workspace configuration domain.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "Workspace")]
public sealed class WorkspaceConfigurationCommand : ConfigurationCommandBase<WorkspaceImplementationConfiguration>
{
    /// <inheritdoc/>
    public WorkspaceConfigurationCommand() : base("Workspace") { }
}
