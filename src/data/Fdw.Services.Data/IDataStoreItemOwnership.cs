using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Data;

/// <summary>
/// The owner of a path or a container of a shared data store (FDW-6967), and the one decision of whose scope a write of one is decided at.
/// </summary>
/// <remarks>
/// The store itself is the platform's and stays unowned; a workspace may own one path or one container of it, recorded as an <c>Owns</c> row of
/// <c>workspace.WorkspaceResource</c> like everything else a workspace owns. An owned item is decided at its owner's scope alone (the workspace's
/// <c>MayWrite</c>), an unowned one by the store's rule as before: the <c>datastores:write</c> permission and the cross-tenant check.
/// </remarks>
public interface IDataStoreItemOwnership
{
    /// <summary>Reads who owns each path and container of one store.</summary>
    /// <param name="storeId">The data store's durable Id.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The owners; a failure when they cannot be read, an item has two owners, or an owning workspace cannot be read.</returns>
    Task<IGenericResult<DataStoreItemOwners>> Owners(Guid storeId, CancellationToken cancellationToken = default);

    /// <summary>Decides whether the caller may change one path or container, at its owner's scope or, when it has none, the store's.</summary>
    /// <param name="storeId">The data store's durable Id.</param>
    /// <param name="item">The item, for messages.</param>
    /// <param name="owner">The owner whose scope decides (<see cref="DataStoreItemOwners.Of"/>); null when the item is the store's.</param>
    /// <param name="cancellationToken">Cancels the decision.</param>
    /// <returns>Success when the caller may write it; the refusal, naming the owner or the store's permission, when not.</returns>
    Task<IGenericResult> MayWrite(Guid storeId, DataStoreItemAddress item, DataStoreItemOwner? owner, CancellationToken cancellationToken = default);

    /// <summary>Makes a workspace the owner of one path or container.</summary>
    /// <param name="item">The path or container.</param>
    /// <param name="workspaceName">The workspace to own it.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>The owner; a refusal when the item or workspace is not there, another workspace owns the item, the workspace is of another tenant,
    /// or the caller may not change the item or the workspace.</returns>
    Task<IGenericResult<DataStoreItemOwner>> Assign(DataStoreItemAddress item, string workspaceName, CancellationToken cancellationToken = default);

    /// <summary>Gives one path or container back to its store.</summary>
    /// <param name="item">The path or container.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>Success; a refusal when the item is not there, has no owner of its own, or the caller may not change it at its owner's scope.</returns>
    Task<IGenericResult> Release(DataStoreItemAddress item, CancellationToken cancellationToken = default);
}
