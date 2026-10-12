using Fdw.Collections;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Who can find a workspace.</summary>
public interface IWorkspaceVisibility : ITypeOption<int, WorkspaceVisibilityBase>
{
    /// <summary>Gets whether the workspace appears in listings to someone who is not a member.</summary>
    bool IsListed { get; }

    /// <summary>Gets whether someone who is not a member may view the workspace (never its rows).</summary>
    bool GrantsView { get; }
}
