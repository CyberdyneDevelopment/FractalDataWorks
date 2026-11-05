using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Multitenancy.Abstractions;

/// <summary>
/// Makes and removes an organization's default workspace (FDW-3668). The workspaces domain implements it, for the same reason it
/// implements <see cref="IPlaceParentSource"/>: a workspace is its business, and the code that founds a tenant or an organization is
/// above it and cannot name it.
/// </summary>
public interface IDefaultWorkspaceFounding
{
    /// <summary>Makes the workspace and its permissions, in the tenant the request names; both, or neither.</summary>
    /// <param name="workspace">The workspace to make.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The workspace's id, or the failure that stopped it. A failure leaves nothing behind, or says what it left.</returns>
    Task<IGenericResult<Guid>> Found(NewDefaultWorkspace workspace, CancellationToken cancellationToken = default);

    /// <summary>Removes a workspace made by <see cref="Found"/> and its permissions: the undo of a founding that failed part way.</summary>
    /// <param name="workspace">The workspace that was made.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success, or the failure that left something behind.</returns>
    Task<IGenericResult> Remove(NewDefaultWorkspace workspace, CancellationToken cancellationToken = default);
}
