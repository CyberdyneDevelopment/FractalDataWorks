using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Workspaces;

/// <summary>
/// The workspaces the calling user may read.
/// </summary>
/// <remarks>
/// The same rule the data route applies to one workspace, <see cref="IWorkspaceAccessPolicy.MayRead"/>,
/// applied to every workspace, so a list of what a caller may query never offers one the route would
/// then refuse. A refusal is not an error here: the policy has already logged why, and the workspace is
/// simply not the caller's to read.
/// </remarks>
public static class ReadableWorkspaces
{
    /// <summary>Gets every workspace the calling user may read.</summary>
    /// <param name="workspaces">Supplies the workspaces, with their members and resources.</param>
    /// <param name="access">Decides whether the caller may read each one.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The readable workspaces, or the failure reading them.</returns>
    public static async Task<IGenericResult<IReadOnlyList<IWorkspaceImplementationConfiguration>>> Get(
        IWorkspaceConfigurationProvider workspaces,
        IWorkspaceAccessPolicy access,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspaces);
        ArgumentNullException.ThrowIfNull(access);

        var all = await workspaces.Get(cancellationToken).ConfigureAwait(false);
        if (all.IsFailure || all.Value is not { } candidates)
            return all;

        var readable = new List<IWorkspaceImplementationConfiguration>();
        foreach (var workspace in candidates)
        {
            var permitted = await access.MayRead(workspace, cancellationToken).ConfigureAwait(false);
            if (permitted.IsSuccess)
                readable.Add(workspace);
            else if (WorkspaceAccessRefusals.IsUndetermined(permitted))
                return permitted.ToNewResult<IReadOnlyList<IWorkspaceImplementationConfiguration>>();
        }

        return GenericResult<IReadOnlyList<IWorkspaceImplementationConfiguration>>.Success(readable);
    }
}
