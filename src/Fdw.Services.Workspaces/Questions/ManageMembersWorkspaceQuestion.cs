using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces;

/// <summary>Whether the caller can decide who is in the workspace: add, change and remove its members, and approve or deny requests to join.</summary>
/// <remarks>
/// Separate from <see cref="WriteWorkspaceQuestion"/> (FDW-1071): changing a project and deciding who
/// belongs to it are different powers. <c>workspaces:write</c> lets a caller change every workspace in
/// the tenant, so it never decides membership; the platform path is <c>workspaces:members</c>.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WorkspaceQuestions), "ManageMembers")]
public sealed class ManageMembersWorkspaceQuestion : WorkspaceQuestionBase
{
    /// <summary>Initializes a new instance of the <see cref="ManageMembersWorkspaceQuestion"/> class.</summary>
    public ManageMembersWorkspaceQuestion()
        : base("ManageMembers", "the caller neither owns it, holds a membership whose role manages members, nor holds workspaces:members")
    {
    }

    /// <inheritdoc />
    public override bool Grants(WorkspaceStanding held)
        => held.IsOwner || held.Holds(WorkspacePermissionNames.ManageMembers) || held.Roles.Any(role => role.MayManageMembers);

    /// <inheritdoc />
    /// <remarks>404 when the caller may not view it; 403 once they may and only managing its members is refused.</remarks>
    public override string RefusalCode(bool views) => views ? "WorkspaceMembersNotManageable" : "WorkspaceNotVisible";
}
