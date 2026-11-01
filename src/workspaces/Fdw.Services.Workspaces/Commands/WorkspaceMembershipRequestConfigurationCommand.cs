using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;

namespace Fdw.Services.Workspaces.Commands;

/// <summary>ConfigurationCommands TypeOption for the WorkspaceMembershipRequest child configuration.</summary>
/// <remarks>Makes membership requests a child of the workspace aggregate, written and read with it.</remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ConfigurationCommands), "WorkspaceMembershipRequest")]
public sealed class WorkspaceMembershipRequestConfigurationCommand : ConfigurationCommandBase<WorkspaceMembershipRequestConfiguration>
{
    /// <inheritdoc/>
    public WorkspaceMembershipRequestConfigurationCommand() : base("WorkspaceMembershipRequest") { }
}
