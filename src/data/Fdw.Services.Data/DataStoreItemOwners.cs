using System;
using System.Collections.Generic;
using Fdw.Services.Connections;

namespace Fdw.Services.Data;

/// <summary>Who owns the paths and containers of one data store, read once for the store (FDW-6967).</summary>
/// <remarks>
/// A path's owner covers the containers under it, unless a container names an owner of its own, which wins for that container (FDW-6967,
/// decision 1). Nothing here is a default: an item with no owner row of its own and none through its path is the store's.
/// </remarks>
public sealed class DataStoreItemOwners
{
    private readonly IReadOnlyDictionary<Guid, DataStoreItemOwner> _paths;
    private readonly IReadOnlyDictionary<Guid, DataStoreItemOwner> _containers;

    /// <summary>Initializes a new instance of the <see cref="DataStoreItemOwners"/> class.</summary>
    /// <param name="tenantId">The tenant the store belongs to.</param>
    /// <param name="paths">The owners of the store's paths, by path Id.</param>
    /// <param name="containers">The owners of the store's containers that have one of their own, by container Id.</param>
    public DataStoreItemOwners(Guid tenantId, IReadOnlyDictionary<Guid, DataStoreItemOwner> paths, IReadOnlyDictionary<Guid, DataStoreItemOwner> containers)
    {
        TenantId = tenantId;
        _paths = paths;
        _containers = containers;
    }

    /// <summary>Gets the tenant the store belongs to.</summary>
    public Guid TenantId { get; }

    /// <summary>The owner recorded on the item itself: the container's own, or the path's when no container is named.</summary>
    /// <param name="path">The path.</param>
    /// <param name="container">The container; null for the path itself.</param>
    /// <returns>The owner; null when the item has no owner row of its own.</returns>
    public DataStoreItemOwner? OwnOf(DataPathConfiguration path, DataContainerConfiguration? container) =>
        container is null ? Find(_paths, path.Id) : Find(_containers, container.Id);

    /// <summary>The owner whose scope decides a write of the item: the container's own, else its path's.</summary>
    /// <param name="path">The path.</param>
    /// <param name="container">The container; null for the path itself.</param>
    /// <returns>The owner; null when the item is the store's.</returns>
    public DataStoreItemOwner? Of(DataPathConfiguration path, DataContainerConfiguration? container)
    {
        if (container is not null && Find(_containers, container.Id) is { } own)
            return own;

        return Find(_paths, path.Id) is { } pathOwner
            ? pathOwner with { ThroughPath = container is not null }
            : null;
    }

    private static DataStoreItemOwner? Find(IReadOnlyDictionary<Guid, DataStoreItemOwner> owners, Guid id) =>
        owners.TryGetValue(id, out var owner) ? owner : null;
}
