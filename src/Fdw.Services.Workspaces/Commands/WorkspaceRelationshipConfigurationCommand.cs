using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Workspaces.Commands;

/// <summary>ConfigurationCommands TypeOption for the WorkspaceRelationship child configuration.</summary>
/// <remarks>See <see cref="WorkspaceResourceConfigurationCommand"/> for why this is safe to register
/// now (FDW-801).</remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "WorkspaceRelationship")]
public sealed class WorkspaceRelationshipConfigurationCommand : ConfigurationCommandBase<WorkspaceRelationshipConfiguration>
{
    /// <inheritdoc/>
    public WorkspaceRelationshipConfigurationCommand() : base("WorkspaceRelationship") { }
}
