using Fdw.Collections;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>
/// Base class for the kinds of resource that can be attached to a workspace.
/// </summary>
/// <remarks>
/// No id is passed. <see cref="TypeOptionBase{TBase}"/> derives one from the option's fully
/// qualified type name, which is what makes options declarable from the packages that own their
/// resources — there is no central place handing out non-colliding numbers, and none is needed.
///
/// Nothing persists the derived id. <c>workspace.WorkspaceResource.ResourceType</c> stores the
/// kind's name, so the name is the part of this contract that must not change once rows exist.
/// </remarks>
public abstract class WorkspaceResourceKindBase
    : TypeOptionBase<WorkspaceResourceKindBase>, IWorkspaceResourceKind
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceResourceKindBase"/> class.
    /// </summary>
    /// <param name="name">The kind name. This is the value persisted in the database.</param>
    /// <param name="canBeOwned">Whether a workspace may own this kind, not merely use it.</param>
    protected WorkspaceResourceKindBase(string name, bool canBeOwned)
        : base(name)
    {
        CanBeOwned = canBeOwned;
    }

    /// <inheritdoc />
    public bool CanBeOwned { get; }
}
