using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Temporarily not in use, but not archived.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceStatuses), "Paused")]
public sealed class PausedWorkspaceStatusOption : WorkspaceStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="PausedWorkspaceStatusOption"/> class.</summary>
    public PausedWorkspaceStatusOption() : base("Paused")
    {
    }
}
