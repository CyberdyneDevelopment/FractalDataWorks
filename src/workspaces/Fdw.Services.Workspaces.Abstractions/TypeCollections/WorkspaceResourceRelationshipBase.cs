using Fdw.Collections;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Base for how a workspace relates to a resource it holds.</summary>
/// <remarks>
/// No id is passed — <see cref="TypeOptionBase{TBase}"/> derives one from the option's fully
/// qualified type name. Nothing persists that id; the database stores the option's NAME, so the
/// name is the part of this contract that must not change once rows exist.
/// </remarks>
public abstract class WorkspaceResourceRelationshipBase
    : TypeOptionBase<WorkspaceResourceRelationshipBase>, IWorkspaceResourceRelationship
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceResourceRelationshipBase"/> class.</summary>
    /// <param name="name">The option name, which is the value persisted.</param>
    protected WorkspaceResourceRelationshipBase(string name) : base(name)
    {
    }
}
