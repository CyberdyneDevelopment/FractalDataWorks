using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Abstractions.CommandCapabilities;

namespace Fdw.Services.Connections.RoslynWorkspace.Abstractions.CommandCapabilities;

/// <summary>
/// Workspace graph capability — retrieves the project-dependency graph from the workspace.
/// Used by RoslynWorkspace connection types.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(CommandCapabilityTypes), "WorkspaceGraph", RestrictToCurrentCompilation = true)]
public sealed class WorkspaceGraphCapability : CommandCapabilityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceGraphCapability"/> class.
    /// </summary>
    public WorkspaceGraphCapability()
        : base(
            id: 8,
            name: "WorkspaceGraph",
            displayName: "Workspace Graph",
            configurationFields: [])
    {
    }
}
