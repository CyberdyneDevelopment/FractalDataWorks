using Fdw.Results;
using Fdw.Services.Workspaces.Abstractions;
using Fdw.Services.Workspaces.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Keeps the workspace owner's own membership row from being removed or demoted.</summary>
/// <remarks>
/// The owner (<see cref="IWorkspaceImplementationConfiguration.OwnerUserId"/>) is always an owner: no
/// endpoint changes it. A member row that names the owner at a lower role, or a removed one, would list
/// members that contradict who owns the workspace, so both are refused with the reason (FDW-1071).
/// </remarks>
internal static class WorkspaceOwnerMembership
{
    /// <summary>Refuses removing the owner's own membership.</summary>
    /// <param name="workspace">The workspace.</param>
    /// <param name="member">The membership being removed.</param>
    /// <param name="logger">The logger.</param>
    internal static IGenericResult MayRemove(IWorkspaceImplementationConfiguration workspace, WorkspaceMemberConfiguration member, ILogger logger)
        => IsOwners(workspace, member) ? Refuse(workspace, "removed", logger) : GenericResult.Success();

    /// <summary>Refuses moving the owner's own membership to a role other than Owner.</summary>
    /// <param name="workspace">The workspace.</param>
    /// <param name="member">The membership being changed.</param>
    /// <param name="newRole">The role it would hold.</param>
    /// <param name="logger">The logger.</param>
    internal static IGenericResult MayChange(
        IWorkspaceImplementationConfiguration workspace, WorkspaceMemberConfiguration member, IWorkspaceMemberRole newRole, ILogger logger)
        => IsOwners(workspace, member) && !ReferenceEquals(newRole, WorkspaceMemberRoles.Owner)
            ? Refuse(workspace, "demoted", logger)
            : GenericResult.Success();

    private static bool IsOwners(IWorkspaceImplementationConfiguration workspace, WorkspaceMemberConfiguration member)
        => ReferenceEquals(WorkspaceSubjectTypes.ByName(member.SubjectType), WorkspaceSubjectTypes.User)
           && member.SubjectId == workspace.OwnerUserId;

    private static IGenericResult Refuse(IWorkspaceImplementationConfiguration workspace, string action, ILogger logger)
        => GenericResult.Failure(
            WorkspacesResultCodes.ByName("WorkspaceOwnerMembershipProtected"), logger,
            ResultDetails.Create("name", workspace.Name, "action", action));
}
