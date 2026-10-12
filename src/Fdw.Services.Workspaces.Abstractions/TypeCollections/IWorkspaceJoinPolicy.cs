using Fdw.Collections;

namespace Fdw.Services.Workspaces.Abstractions;

/// <summary>What happens when someone asks to join a workspace.</summary>
public interface IWorkspaceJoinPolicy : ITypeOption<int, WorkspaceJoinPolicyBase>
{
    /// <summary>Gets whether this policy accepts a membership request at all.</summary>
    bool AcceptsRequests { get; }

    /// <summary>Gets whether an accepted request grants membership immediately, with no review.</summary>
    bool AutoApproves { get; }
}
