using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Puts the owner of each path and container on the responses that show them (FDW-6967).</summary>
/// <remarks>The owners are read once for the store, then laid on the mapped responses by Id; a response with no owner is the store's.</remarks>
internal static class DataStoreItemOwnerReads
{
    /// <summary>Reads who owns the paths and containers of the store with this name.</summary>
    /// <param name="stores">Reads the store's record for its Id.</param>
    /// <param name="ownership">Reads the owners.</param>
    /// <param name="storeName">The store's name.</param>
    /// <param name="logger">The logger a store that is not there is reported to.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The owners; a failure when the store or its owners cannot be read.</returns>
    internal static async Task<IGenericResult<DataStoreItemOwners>> Of(
        IDataStoreConfigurationProvider stores, IDataStoreItemOwnership ownership, string storeName, ILogger logger, CancellationToken ct)
    {
        var store = await stores.Get(storeName, ConfigurationLevels.RecordOnly, ct).ConfigureAwait(false);
        if (store.IsFailure) return store.ToNewResult<DataStoreItemOwners>();
        if (store.Value is null) return GenericResult<DataStoreItemOwners>.Failure(DataStoreItemOwnershipLog.StoreNotFound(logger, storeName));

        return await ownership.Owners(store.Value.Id, ct).ConfigureAwait(false);
    }

    /// <summary>Lays the owners on mapped paths, matching each to its configuration by Id.</summary>
    /// <param name="mapped">The mapped paths.</param>
    /// <param name="paths">The paths as read.</param>
    /// <param name="owners">The store's owners.</param>
    internal static void ShowAll(IEnumerable<DataStorePathResponse> mapped, IEnumerable<DataPathConfiguration> paths, DataStoreItemOwners owners)
    {
        var byId = paths.ToDictionary(path => path.Id);
        foreach (var response in mapped)
        {
            if (byId.TryGetValue(response.Id, out var path))
                Show(response, path, owners);
        }
    }

    /// <summary>Lays the owners on one mapped path and its mapped containers.</summary>
    /// <param name="mapped">The mapped path.</param>
    /// <param name="path">The path as read.</param>
    /// <param name="owners">The store's owners.</param>
    /// <returns>The mapped path, with its owners.</returns>
    internal static DataStorePathResponse Show(DataStorePathResponse mapped, DataPathConfiguration path, DataStoreItemOwners owners)
    {
        mapped.Owner = DataStoreItemOwnerResponse.From(owners.Of(path, null));
        var containers = path.Containers.ToDictionary(container => container.Id);
        foreach (var response in mapped.Containers)
        {
            if (containers.TryGetValue(response.Id, out var container))
                response.Owner = DataStoreItemOwnerResponse.From(owners.Of(path, container));
        }

        return mapped;
    }
}
