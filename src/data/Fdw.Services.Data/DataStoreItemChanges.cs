using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>What a write of a data store touches: the paths and containers it adds, changes or removes, and whether it changes the store's own fields (FDW-6968).</summary>
/// <remarks>
/// <para>
/// Every write of a path or a container is a save of the whole store, so the one place that can tell which items a write touches is a comparison of
/// the store as stored with the store as written. A path is compared without its containers, a container whole (its fields, keys and paging with it),
/// matched by Id. A comparison errs towards "changed": a value that reads back differently counts as a change and is decided, never skipped.
/// </para>
/// <para>
/// A comparison that cannot be made fails loud: a write whose touched items are not known is not written.
/// </para>
/// </remarks>
public sealed class DataStoreItemChanges
{
    private static readonly JsonSerializerOptions Options = new() { ReferenceHandler = ReferenceHandler.IgnoreCycles };

    private DataStoreItemChanges(bool storeChanged, IReadOnlyList<DataStoreItemChange> items)
    {
        StoreChanged = storeChanged;
        Items = items;
    }

    /// <summary>Gets a value indicating whether the store's own fields change.</summary>
    public bool StoreChanged { get; }

    /// <summary>Gets the paths and containers the write adds, changes or removes.</summary>
    public IReadOnlyList<DataStoreItemChange> Items { get; }

    /// <summary>Compares the store as stored with the store as written.</summary>
    /// <param name="stored">The store as it is stored now.</param>
    /// <param name="written">The store as the write would save it.</param>
    /// <param name="logger">The logger a comparison that cannot be made is reported to.</param>
    /// <returns>What the write touches; a failure when the two cannot be compared.</returns>
    public static IGenericResult<DataStoreItemChanges> Between(IDataStoreImplementationConfiguration stored, IDataStoreImplementationConfiguration written, ILogger logger)
    {
        try
        {
            var items = new List<DataStoreItemChange>();
            var storedPaths = stored.Paths.ToDictionary(path => path.Id);
            var writtenPaths = written.Paths.ToDictionary(path => path.Id);

            foreach (var path in written.Paths)
            {
                storedPaths.TryGetValue(path.Id, out var before);
                Compare(path, before, items);
            }

            foreach (var path in stored.Paths.Where(path => !writtenPaths.ContainsKey(path.Id)))
                Removed(path, items);

            return GenericResult<DataStoreItemChanges>.Success(new DataStoreItemChanges(!string.Equals(Own(stored), Own(written), StringComparison.Ordinal), items));
        }
        catch (Exception ex) when (ex is NotSupportedException or JsonException or InvalidOperationException or ArgumentException)
        {
            return GenericResult<DataStoreItemChanges>.Failure(DataStoreItemOwnershipLog.ChangeNotCompared(logger, ex, written.Name));
        }
    }

    /// <summary>Every path and container of a store, as a delete of the store touches them all.</summary>
    /// <param name="stored">The store as it is stored now.</param>
    /// <returns>Every item of the store.</returns>
    public static DataStoreItemChanges Every(IDataStoreImplementationConfiguration stored)
    {
        var items = new List<DataStoreItemChange>();
        foreach (var path in stored.Paths)
            Removed(path, items);

        return new DataStoreItemChanges(true, items);
    }

    // A path the write keeps or adds: the path itself when its own record differs, then each container that differs, is new, or is gone.
    private static void Compare(DataPathConfiguration path, DataPathConfiguration? before, List<DataStoreItemChange> items)
    {
        if (before is null)
        {
            items.Add(new DataStoreItemChange(path, null));
            items.AddRange(path.Containers.Select(container => new DataStoreItemChange(path, container)));
            return;
        }

        if (!string.Equals(PathRecord(path), PathRecord(before), StringComparison.Ordinal))
            items.Add(new DataStoreItemChange(path, null));

        var storedContainers = before.Containers.ToDictionary(container => container.Id);
        var writtenIds = path.Containers.Select(container => container.Id).ToHashSet();
        foreach (var container in path.Containers)
        {
            if (!storedContainers.TryGetValue(container.Id, out var was)
                || !string.Equals(JsonSerializer.Serialize(container, Options), JsonSerializer.Serialize(was, Options), StringComparison.Ordinal))
            {
                items.Add(new DataStoreItemChange(path, container));
            }
        }

        items.AddRange(before.Containers.Where(container => !writtenIds.Contains(container.Id)).Select(container => new DataStoreItemChange(before, container)));
    }

    // A path the write removes, and every container under it.
    private static void Removed(DataPathConfiguration path, List<DataStoreItemChange> items)
    {
        items.Add(new DataStoreItemChange(path, null));
        items.AddRange(path.Containers.Select(container => new DataStoreItemChange(path, container)));
    }

    // A path's own record, without the containers under it, which are compared one by one.
    private static string PathRecord(DataPathConfiguration path) => Without(JsonSerializer.SerializeToNode(path, Options), nameof(DataPathConfiguration.Containers));

    // A store's own record, without its paths.
    private static string Own(IDataStoreImplementationConfiguration store) =>
        Without(JsonSerializer.SerializeToNode(store, store.GetType(), Options), nameof(IDataStoreImplementationConfiguration.Paths));

    private static string Without(JsonNode? node, string property)
    {
        if (node is JsonObject record)
            record.Remove(property);

        return node is null ? string.Empty : node.ToJsonString();
    }
}
