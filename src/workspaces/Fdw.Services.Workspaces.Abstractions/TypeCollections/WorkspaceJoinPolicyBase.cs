using Fdw.Collections;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>Base for What happens when someone asks to join a workspace.</summary>
/// <remarks>
/// No id is passed — <see cref="TypeOptionBase{TBase}"/> derives one from the option's fully
/// qualified type name. Nothing persists that id; the database stores the option's NAME, so the
/// name is the part of this contract that must not change once rows exist.
/// </remarks>
public abstract class WorkspaceJoinPolicyBase : TypeOptionBase<WorkspaceJoinPolicyBase>, IWorkspaceJoinPolicy
{
    /// <summary>Initializes a new instance of the <see cref="WorkspaceJoinPolicyBase"/> class.</summary>
    /// <param name="name">The option name, which is the value persisted.</param>
    /// <param name="acceptsRequests">Whether this policy accepts a membership request at all.</param>
    /// <param name="autoApproves">Whether an accepted request grants membership immediately, with no review.</param>
    protected WorkspaceJoinPolicyBase(string name, bool acceptsRequests, bool autoApproves) : base(name)
    {
        AcceptsRequests = acceptsRequests;
        AutoApproves = autoApproves;
    }

    /// <summary>Gets whether this policy accepts a membership request at all.</summary>
    public bool AcceptsRequests { get; }

    /// <summary>Gets whether an accepted request grants membership immediately, with no review.</summary>
    public bool AutoApproves { get; }
}
