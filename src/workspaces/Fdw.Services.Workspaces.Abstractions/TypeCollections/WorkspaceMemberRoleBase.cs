using Fdw.Collections;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Base for the roles a person can hold in a workspace.</summary>
/// <remarks>
/// No id is passed — it derives from the option's fully qualified type name. The database stores
/// the NAME, so the name is the part of this contract that must not change once rows exist.
/// </remarks>
public abstract class WorkspaceMemberRoleBase : TypeOptionBase<WorkspaceMemberRoleBase>, IWorkspaceMemberRole
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceMemberRoleBase"/> class.</summary>
    /// <param name="name">The role name, which is the value persisted.</param>
    /// <param name="mayView">Whether this role may open the workspace: its page, map, members and notes.</param>
    /// <param name="mayRead">Whether this role may query the data the workspace holds.</param>
    /// <param name="mayWrite">Whether this role may change the workspace.</param>
    /// <param name="mayManageMembers">Whether this role may add, change and remove the workspace's members.</param>
    /// <param name="rank">The role's standing relative to the others; a higher rank outranks a lower one.</param>
    protected WorkspaceMemberRoleBase(string name, bool mayView, bool mayRead, bool mayWrite, bool mayManageMembers, int rank) : base(name)
    {
        MayView = mayView;
        MayRead = mayRead;
        MayWrite = mayWrite;
        MayManageMembers = mayManageMembers;
        Rank = rank;
    }

    /// <summary>Gets whether this role may query the data sets attached to the workspace it is held in.</summary>
    /// <remarks>
    /// The read-side twin of <see cref="MayWrite"/>, answered by the option for the same reason: a
    /// role added later states whether it reads rather than inheriting a caller's guess (FDW-876).
    /// </remarks>
    public bool MayRead { get; }

    /// <summary>Gets whether this role may open the workspace it is held in: its page, map, members and notes.</summary>
    /// <remarks>
    /// Separate from <see cref="MayRead"/> so a member can be let in to look without being handed the
    /// rows: the Observer role views and does not read (FDW-937). Every role that reads also views.
    /// </remarks>
    public bool MayView { get; }

    /// <summary>Gets whether this role may change the workspace it is held in.</summary>
    /// <remarks>
    /// On the option, not decided by a caller comparing names, so a role added later arrives with
    /// its own answer instead of falling through somebody's else-branch. Together with
    /// <see cref="MayRead"/> these answer what a member may do to the workspace; who else is in it is
    /// <see cref="MayManageMembers"/>, a separate answer, so changing a project does not imply deciding
    /// who belongs to it.
    /// </remarks>
    public bool MayWrite { get; }

    /// <summary>Gets whether this role may add, change and remove the members of the workspace it is held in.</summary>
    /// <remarks>
    /// Separate from <see cref="MayWrite"/> (FDW-1071): a Contributor changes the project, an Owner or a
    /// Steward decides who is in it. A member who manages members grants at most their own
    /// <see cref="Rank"/>.
    /// </remarks>
    public bool MayManageMembers { get; }

    /// <summary>Gets the role's rank among the member roles: a member may grant or change only roles at or below their own.</summary>
    /// <remarks>
    /// Read off the option rather than compared by name, so a role added later states where it stands.
    /// The NotFound sentinel has rank 0 and outranks nothing.
    /// </remarks>
    public int Rank { get; }
}
