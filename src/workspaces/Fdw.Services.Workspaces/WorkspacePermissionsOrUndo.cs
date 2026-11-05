using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Workspaces.Logging;
using Fdw.Services.Workspaces.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces;

/// <summary>
/// The step that follows saving a new workspace: its own permissions are created, and when that fails the workspace and whatever
/// permissions were made are removed again, so a workspace nobody can be given access to is never left. The one place this is written, for
/// the create endpoint and for the founding of a default workspace alike.
/// </summary>
public static class WorkspacePermissionsOrUndo
{
    /// <summary>Creates the permissions of a workspace that was just saved, or undoes the save.</summary>
    /// <param name="permissions">The workspace permissions service.</param>
    /// <param name="workspaces">The workspace provider, which deletes the workspace again on a failure.</param>
    /// <param name="workspaceId">The saved workspace's id.</param>
    /// <param name="workspaceName">The saved workspace's name.</param>
    /// <param name="logger">The caller's logger, so the lines read as the caller's.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success; the permissions failure when the undo worked; or a compensation failure naming what was left.</returns>
    public static async Task<IGenericResult> Create(
        IWorkspacePermissions permissions,
        IWorkspaceConfigurationProvider workspaces,
        Guid workspaceId,
        string workspaceName,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(permissions);
        ArgumentNullException.ThrowIfNull(workspaces);
        ArgumentNullException.ThrowIfNull(logger);

        var created = await permissions.Create(workspaceId, workspaceName, cancellationToken).ConfigureAwait(false);
        if (created.IsSuccess) return created;

        WorkspaceLifecycleLog.UndoingCreate(logger, workspaceName, string.Join(" <- ", created.CodeChain.Select(code => code.Name)));
        var retired = await permissions.Remove(workspaceId, cancellationToken).ConfigureAwait(false);
        var deleted = await workspaces.Delete(workspaceId, cancellationToken).ConfigureAwait(false);
        if (retired.IsSuccess && deleted.IsSuccess)
        {
            WorkspaceLifecycleLog.CreateUndone(logger, workspaceName);
            return created;
        }

        var id = workspaceId.ToString("D", CultureInfo.InvariantCulture);
        var left = (retired.IsSuccess, deleted.IsSuccess) switch
        {
            (false, false) => $"the workspace and its permissions workspaces/{id}:*",
            (false, true) => $"its permissions workspaces/{id}:*",
            _ => "the workspace",
        };
        return GenericResult.Failure(
            WorkspacesResultCodes.ByName("WorkspaceCompensationFailed"), logger,
            ResultDetails.Create("name", workspaceName, "step", "creating its permissions", "undo", left));
    }
}
