using System;
using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Results;
using Fdw.Results;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.DataNodes;

/// <summary>
/// Runtime implementation of <see cref="IDataStore"/>.
/// Eagerly constructed with a path list; <see cref="Path"/> does O(1) dictionary lookup by name.
/// </summary>
internal sealed class DataStore : IDataStore
{
    private Dictionary<string, IDataPath> _pathIndex;
    private bool _pathsFinalized;

    // Why: the node holds a real ILogger so its navigation-miss log calls actually emit. Passing
    // NullLogger.Instance to a built log message is a no-op (the message never reaches a sink), so
    // the builder threads its logger in via the constructor. NullLogger fallback is the only ?? allowed.
    private readonly ILogger _logger;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Guid ConnectionId { get; }

    /// <inheritdoc />
    public IReadOnlyList<IDataPath> Paths { get; private set; }

    /// <inheritdoc />
    public string? Description { get; }

    /// <inheritdoc />
    // Why: a store's child nodes ARE its paths — the typed Paths view over the uniform child surface.
    public IReadOnlyList<IDataNode> Nodes => Paths;

    /// <inheritdoc />
    public IGenericResult<IDataNode> Node(string name)
    {
        if (_pathIndex.TryGetValue(name, out var path))
            return GenericResult<IDataNode>.Success(path);

        return PathNotFoundResult<IDataNode>(name);
    }

    internal DataStore(string name, Guid connectionId, IReadOnlyList<IDataPath> paths, string? description = null, ILogger? logger = null)
    {
        Name = name;
        ConnectionId = connectionId;
        Paths = paths;
        Description = description;
        // Why: NullLogger keeps the node functional without DI logging — the only sanctioned ?? fallback.
        _logger = logger ?? NullLogger.Instance;

        // Why: O(1) lookup dictionary — path names are unique within a store.
        _pathIndex = BuildIndex(paths);
    }

    // Why (chicken-and-egg fix, one level up from DataPath.SetContainers): a path needs its owning store
    // at construction, but the store's path index needs the paths — so the builder constructs the FINAL
    // store first (empty), builds every path under THIS store object, then calls SetPaths to wire the
    // index. Without this both DataPath sites were built with `store: null!`, leaving IDataPath.Store —
    // declared NON-nullable, and enforced as such by DetachedDataPath — null on every runtime path, which
    // turned DataPath.ContainerNotFoundResult into a NullReferenceException instead of a failure result.
    // Set-once: finalized exactly once (a second call is a wiring defect — fail loud).
    internal void SetPaths(IReadOnlyList<IDataPath> paths)
    {
        if (paths is null)
            throw new ArgumentNullException(nameof(paths));
        if (_pathsFinalized)
            throw new InvalidOperationException($"DataStore '{Name}' paths already finalized.");

        _pathsFinalized = true;
        Paths = paths;
        _pathIndex = BuildIndex(paths);
    }

    private static Dictionary<string, IDataPath> BuildIndex(IReadOnlyList<IDataPath> paths)
    {
        var index = new Dictionary<string, IDataPath>(StringComparer.Ordinal);
        foreach (var p in paths)
        {
            index.TryAdd(p.Name, p);
        }

        return index;
    }

    /// <inheritdoc />
    public IGenericResult<IDataPath> Path(string name)
    {
        if (_pathIndex.TryGetValue(name, out var path))
            return GenericResult<IDataPath>.Success(path);

        return PathNotFoundResult<IDataPath>(name);
    }

    // Why: ONE construction point for "this store registers no such path", shared by Node and Path so the
    // two navigation surfaces cannot drift. The failure carries the typed DataPathNotFound code CHAINED
    // over the node's own Debug navigation message: callers that need to branch on the structural cause
    // read Code/CodeChain (never message text — see DataPathNotFoundCode), while CurrentMessage and the
    // Debug log line stay exactly what they were before the code was attached.
    private IGenericResult<T> PathNotFoundResult<T>(string name) =>
        GenericResult<T>.Chain(
            DataStoresResultCodes.DataPathNotFound,
            GenericResult.Failure(DataStoreLoaderLog.PathNotFound(_logger, name, Name)),
            ResultDetails.Create("PathName", name, "DataStoreName", Name));
}
