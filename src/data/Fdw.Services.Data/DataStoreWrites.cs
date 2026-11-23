using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Decides and makes the writes of a data store that exists (FDW-6968): what the write touches is read from the stored store, each touched path and
/// container is decided at its owner's scope or the store's, and only then is the store written.
/// </summary>
/// <remarks>
/// <para>
/// Each scope is asked once however many of its items the write touches. An owned item is decided at its owner's scope alone, with no
/// platform-administrator override (FDW-6964, decision 3); a write that touches only owned items is not asked the store's rule, so a member of the
/// owner who holds no store permission can write the container the workspace owns. A change to the store's own fields, or to an item nobody owns,
/// asks the store's rule as before. A write that touches both needs each to pass.
/// </para>
/// <para>
/// Takes the provider through <see cref="IDataStoreUncheckedWrites"/>, internal to this package: the provider's public interface no longer offers the
/// unchecked writes, and this is the one place that makes them.
/// </para>
/// </remarks>
public sealed class DataStoreWrites : IDataStoreWrites
{
    private readonly IDataStoreUncheckedWrites _stores;
    private readonly IDataStoreItemOwnership _ownership;
    private readonly ILogger<DataStoreWrites> _logger;

    /// <summary>Initializes a new instance of the <see cref="DataStoreWrites"/> class.</summary>
    /// <param name="stores">Reads the stored store and makes the write: the data store provider, through the writes it does not offer a caller.</param>
    /// <param name="ownership">Reads the owners and decides each scope.</param>
    /// <param name="logger">The logger.</param>
    internal DataStoreWrites(IDataStoreUncheckedWrites stores, IDataStoreItemOwnership ownership, ILogger<DataStoreWrites>? logger = null)
    {
        _stores = stores;
        _ownership = ownership;
        _logger = logger ?? NullLogger<DataStoreWrites>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult> UpdateStore(IDataStoreImplementationConfiguration store, CancellationToken cancellationToken = default)
    {
        var stored = await Stored(store.Id, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure) return stored;

        var changes = DataStoreItemChanges.Between(stored.Value!, store, _logger);
        if (changes.IsFailure) return changes;

        var permitted = await Decide(store, changes.Value!, cancellationToken).ConfigureAwait(false);
        return permitted.IsFailure
            ? permitted
            : await _stores.Update(store, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> DeleteStore(Guid storeId, CancellationToken cancellationToken = default)
    {
        var stored = await Stored(storeId, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure) return stored;

        var permitted = await Decide(stored.Value!, DataStoreItemChanges.Every(stored.Value!), cancellationToken).ConfigureAwait(false);
        return permitted.IsFailure
            ? permitted
            : await _stores.Delete(storeId, cancellationToken).ConfigureAwait(false);
    }

    // The store as it is stored now, whole: its paths, containers, fields, keys and paging.
    private async Task<IGenericResult<IDataStoreImplementationConfiguration>> Stored(Guid storeId, CancellationToken cancellationToken)
    {
        var stored = await _stores.Stored(storeId, cancellationToken).ConfigureAwait(false);
        if (stored.IsFailure) return stored;

        return stored.Value is null
            ? GenericResult<IDataStoreImplementationConfiguration>.Failure(
                DataStoreItemOwnershipLog.StoreNotFoundForWrite(_logger, storeId.ToString("D", CultureInfo.InvariantCulture)))
            : stored;
    }

    // Each scope the write touches, asked once: the owner of each owned item, and the store for its own fields, an item nobody owns, or a write that
    // touches nothing (a save that changes nothing is still a write of the store).
    private async Task<IGenericResult> Decide(IDataStoreImplementationConfiguration store, DataStoreItemChanges changes, CancellationToken cancellationToken)
    {
        var owners = await _ownership.Owners(store.Id, cancellationToken).ConfigureAwait(false);
        if (owners.IsFailure) return owners;

        var byOwner = new Dictionary<Guid, (DataStoreItemAddress Item, DataStoreItemOwner Owner)>();
        DataStoreItemAddress? storeItem = changes.StoreChanged || changes.Items.Count == 0
            ? new DataStoreItemAddress(store.Name, string.Empty, null)
            : null;

        foreach (var change in changes.Items)
        {
            var address = new DataStoreItemAddress(store.Name, change.Path.Name, change.Container?.Name);
            if (owners.Value!.Of(change.Path, change.Container) is { } owner)
            {
                byOwner.TryAdd(owner.WorkspaceId, (address, owner));
            }
            else
            {
                storeItem ??= address;
            }
        }

        DataStoreItemOwnershipLog.WriteDecided(_logger, store.Name, changes.Items.Count, byOwner.Count + (storeItem is null ? 0 : 1));

        foreach (var (item, owner) in byOwner.Values)
        {
            var permitted = await _ownership.MayWrite(store.Id, item, owner, cancellationToken).ConfigureAwait(false);
            if (permitted.IsFailure) return permitted;
        }

        return storeItem is null
            ? GenericResult.Success()
            : await _ownership.MayWrite(store.Id, storeItem, null, cancellationToken).ConfigureAwait(false);
    }
}
