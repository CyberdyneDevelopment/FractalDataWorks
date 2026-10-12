using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Workspaces.Commands;

/// <summary>ConfigurationCommands TypeOption for the WorkspaceResource child configuration.</summary>
/// <remarks>
/// Closes FDW-801's DELETE side: ImplementationProviderBase.RetireCollections resolves this via
/// ConfigurationCommands.All() to retire a workspace's attached resources when the workspace itself
/// is deleted. UpdateWorkspaceEndpointBase clears its collections before Save (never routes a
/// field-only PATCH through the cascade-save path this same lookup backs), so registering this does
/// not reintroduce the duplicate-insert-on-every-PATCH problem that path would otherwise hit.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "WorkspaceResource")]
public sealed class WorkspaceResourceConfigurationCommand : ConfigurationCommandBase<WorkspaceResourceConfiguration>
{
    /// <inheritdoc/>
    public WorkspaceResourceConfigurationCommand() : base("WorkspaceResource") { }
}
