using Fdw.Collections;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Base for the states a membership request can be in.</summary>
/// <remarks>
/// No id is passed — it derives from the option's fully qualified type name. The database stores
/// the NAME, so the name is the part of this contract that must not change once rows exist.
/// </remarks>
public abstract class WorkspaceMembershipRequestStatusBase : TypeOptionBase<WorkspaceMembershipRequestStatusBase>, IWorkspaceMembershipRequestStatus
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceMembershipRequestStatusBase"/> class.</summary>
    /// <param name="name">The status name, which is the value persisted.</param>
    protected WorkspaceMembershipRequestStatusBase(string name) : base(name)
    {
    }
}
