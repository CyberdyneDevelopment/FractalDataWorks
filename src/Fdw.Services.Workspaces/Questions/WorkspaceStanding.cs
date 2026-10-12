using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Services.Workspaces.Abstractions;

namespace Fdw.Services.Workspaces;

/// <summary>
/// What a caller holds in one workspace, worked out once so every question (and the choice between a
/// 404 and a 403) is answered from the same facts.
/// </summary>
/// <param name="WorkspaceId">The workspace's durable Id, which names its own permissions.</param>
/// <param name="IsOwner">Whether the caller owns the workspace.</param>
/// <param name="Permissions">The permissions baked into the caller's token.</param>
/// <param name="Roles">The roles of the caller's memberships that are in effect in this workspace.</param>
/// <param name="Visibility">The workspace's visibility.</param>
public sealed record WorkspaceStanding(
    Guid WorkspaceId,
    bool IsOwner,
    IReadOnlyCollection<string> Permissions,
    IReadOnlyList<IWorkspaceMemberRole> Roles,
    IWorkspaceVisibility Visibility)
{
    /// <summary>
    /// Whether the caller holds <paramref name="permission"/>: the exact permission, the resource's
    /// wildcard, or the all-resources wildcard -- the same test the platform's own gate applies
    /// (<c>DefaultAuthorizationService</c>).
    /// </summary>
    /// <param name="permission">A platform permission, e.g. <c>workspaces:read</c>.</param>
    /// <returns><c>true</c> when the caller holds it.</returns>
    public bool Holds(string permission)
        => Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase)
           || Permissions.Contains("workspaces:*", StringComparer.OrdinalIgnoreCase)
           || Permissions.Contains("*:*", StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Whether the caller holds the permission for this one workspace: <c>workspaces/{id}:{action}</c>,
    /// or its wildcard <c>workspaces/{id}:*</c>.
    /// </summary>
    /// <param name="action">The action, <see cref="WorkspacePermissionNames.ViewAction"/> or <see cref="WorkspacePermissionNames.QueryAction"/>.</param>
    /// <returns><c>true</c> when the caller holds it.</returns>
    /// <remarks>
    /// Granted to roles like any other permission, so access to a single workspace can be given without
    /// a membership and without the platform-wide permission. Keyed by the workspace's Id, not its name
    /// (FDW-937): names are unique only within a tenant while permission names are unique across all of
    /// them, and a name-keyed permission would reveal another tenant's workspace.
    /// </remarks>
    public bool HoldsForThis(string action)
        => Permissions.Contains(WorkspacePermissionNames.For(WorkspaceId, action), StringComparer.OrdinalIgnoreCase)
           || Permissions.Contains(WorkspacePermissionNames.For(WorkspaceId, "*"), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Whether the caller may grant, change or remove <paramref name="role"/>: the owner and holders of
    /// <c>workspaces:members</c> may handle every role; a member may handle only roles at or below the
    /// highest rank they hold through a role that manages members (FDW-1071).
    /// </summary>
    /// <param name="role">The role being granted, or the role a member holds now.</param>
    /// <returns><c>true</c> when the caller stands at or above it.</returns>
    public bool Outranks(IWorkspaceMemberRole role)
        => IsOwner || Holds(WorkspacePermissionNames.ManageMembers)
           || Roles.Any(held => held.MayManageMembers && held.Rank >= role.Rank);
}
