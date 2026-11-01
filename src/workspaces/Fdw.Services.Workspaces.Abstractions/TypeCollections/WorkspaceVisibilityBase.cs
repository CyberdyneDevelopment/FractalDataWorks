using Fdw.Collections;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Base for Who can find a workspace.</summary>
/// <remarks>
/// No id is passed — <see cref="TypeOptionBase{TBase}"/> derives one from the option's fully
/// qualified type name. Nothing persists that id; the database stores the option's NAME, so the
/// name is the part of this contract that must not change once rows exist.
/// </remarks>
public abstract class WorkspaceVisibilityBase : TypeOptionBase<WorkspaceVisibilityBase>, IWorkspaceVisibility
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceVisibilityBase"/> class.</summary>
    /// <param name="name">The option name, which is the value persisted.</param>
    /// <param name="isListed">Whether the workspace appears to someone who is not a member.</param>
    /// <param name="grantsView">Whether someone who is not a member may view it.</param>
    protected WorkspaceVisibilityBase(string name, bool isListed, bool grantsView) : base(name)
    {
        IsListed = isListed;
        GrantsView = grantsView;
    }

    /// <summary>Gets whether the workspace appears in listings to someone who is not a member.</summary>
    /// <remarks>
    /// Listed is not viewable: a listed workspace can be found and asked to join, and still shows
    /// nothing past its name and description to a non-member unless <see cref="GrantsView"/> says so.
    /// On the option rather than decided by a caller comparing names, so a visibility added later
    /// arrives with its own answer (FDW-936).
    /// </remarks>
    public bool IsListed { get; }

    /// <summary>Gets whether someone who is not a member may view the workspace.</summary>
    /// <remarks>
    /// Viewing is the workspace itself -- its page, map, readiness, members, notes and data views.
    /// It never includes the rows of the data sets attached to it: querying those needs ownership,
    /// a membership whose role reads, or the platform permission, so a key confined by membership to
    /// one workspace cannot read another just because it is visible (FDW-876, FDW-936).
    /// </remarks>
    public bool GrantsView { get; }
}
