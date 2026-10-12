using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Workspaces;

/// <summary>
/// Creates and removes the permissions that belong to one workspace.
/// </summary>
/// <remarks>
/// A workspace's own permissions (<see cref="WorkspacePermissionNames"/>) are ordinary permission
/// rows, so an administrator scopes a team to one workspace by granting its role those permissions
/// through the usual role-permission path. They exist exactly as long as the workspace does, and are
/// named by its Id, so a permission can never outlive its workspace into another one (FDW-937).
/// </remarks>
public interface IWorkspacePermissions
{
    /// <summary>Creates the workspace's permissions. Idempotent: a permission that already exists is left as it is.</summary>
    /// <param name="workspaceId">The workspace's durable Id, which the permissions are named by.</param>
    /// <param name="workspaceName">The workspace's name, for the permissions' display names only.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success once both exist, or the failure that stopped it.</returns>
    Task<IGenericResult> Create(Guid workspaceId, string workspaceName, CancellationToken cancellationToken = default);

    /// <summary>Removes the workspace's permissions and every role grant of them. Idempotent.</summary>
    /// <param name="workspaceId">The workspace's durable Id.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success once neither the permissions nor their grants remain, or the failure that stopped it.</returns>
    Task<IGenericResult> Remove(Guid workspaceId, CancellationToken cancellationToken = default);
}
