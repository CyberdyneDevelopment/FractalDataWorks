using System;
using System.Linq;
using Fdw.Services.Workspaces;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Adds the membership an approved request grants to the workspace aggregate.</summary>
/// <remarks>
/// Shared by <see cref="ApproveWorkspaceMembershipRequestEndpointBase"/> and
/// <see cref="RequestWorkspaceMembershipEndpointBase"/> (auto-approval). The member is a child of the
/// workspace, so it is written by the caller's save of the workspace, with the request it came from.
/// </remarks>
internal static class WorkspaceMembershipGrant
{
    /// <summary>Adds the member the request names, unless the subject is already a member.</summary>
    /// <param name="workspace">The workspace aggregate, read with its members.</param>
    /// <param name="approved">The approved request.</param>
    /// <param name="grantedRole">
    /// The role granted: the reviewer's choice, or the workspace's AutoApproveRole. Never read off the
    /// request, so asking for Owner does not make anyone an Owner (FDW-1070).
    /// </param>
    /// <param name="grantedByUserId">Who granted the membership.</param>
    /// <param name="now">When it was granted.</param>
    public static void Grant(
        IWorkspaceImplementationConfiguration workspace, WorkspaceMembershipRequestConfiguration approved, string grantedRole, Guid grantedByUserId, DateTimeOffset now)
    {
        if (workspace.Members.Any(x => string.Equals(x.SubjectType, approved.SubjectType, StringComparison.Ordinal) && x.SubjectId == approved.SubjectId))
            return;

        workspace.Members.Add(new WorkspaceMemberConfiguration
        {
            Id = Guid.CreateVersion7(),
            Name = $"{approved.SubjectType}:{approved.SubjectId}",
            SubjectType = approved.SubjectType,
            SubjectId = approved.SubjectId,
            MemberRole = grantedRole,
            State = "Active",
            JoinedAt = now,
            InvitedByUserId = grantedByUserId,
            CreateDate = now,
        });
    }
}
