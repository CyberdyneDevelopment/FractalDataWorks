using Fdw.Collections;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>A role a person can hold in a workspace.</summary>
public interface IWorkspaceMemberRole : ITypeOption<int, WorkspaceMemberRoleBase>
{
    /// <summary>Gets whether this role may query the data sets attached to the workspace it is held in.</summary>
    bool MayRead { get; }

    /// <summary>Gets whether this role may open the workspace it is held in: its page, map, members and notes, not its rows.</summary>
    bool MayView { get; }

    /// <summary>Gets whether this role may change the workspace it is held in.</summary>
    bool MayWrite { get; }

    /// <summary>Gets whether this role may add, change and remove the members of the workspace it is held in.</summary>
    bool MayManageMembers { get; }

    /// <summary>Gets the role's rank: a member may grant or change only roles at or below their own.</summary>
    int Rank { get; }
}
