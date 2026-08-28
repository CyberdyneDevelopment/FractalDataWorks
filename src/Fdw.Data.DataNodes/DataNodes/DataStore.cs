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
    private Dictionary<string, IDataNodePath> _pathIndex;
    private bool _pathsFinalized;

    private readonly ILogger _logger;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Guid ConnectionId { get; }

    /// <inheritdoc />
    public IReadOnlyList<IDataNodePath> Paths { get; private set; }

    /// <inheritdoc />
    public string? Description { get; }

    /// <inheritdoc />
    public IReadOnlyList<IDataNode> Nodes => Paths;

    /// <inheritdoc />
    public IGenericResult<IDataNode> Node(string name)
    {
        if (_pathIndex.TryGetValue(name, out var path))
            return GenericResult<IDataNode>.Success(path);

        return PathNotFoundResult<IDataNode>(name);
    }

    internal DataStore(string name, Guid connectionId, IReadOnlyList<IDataNodePath> paths, string? description = null, ILogger? logger = null)
    {
        Name = name;
        ConnectionId = connectionId;
        Paths = paths;
        Description = description;
        _logger = logger ?? NullLogger.Instance;

        _pathIndex = BuildIndex(paths);
    }

    internal void SetPaths(IReadOnlyList<IDataNodePath> paths)
    {
        if (paths is null)
            throw new ArgumentNullException(nameof(paths));
        if (_pathsFinalized)
            throw new InvalidOperationException($"DataStore '{Name}' paths already finalized.");

        _pathsFinalized = true;
        Paths = paths;
        _pathIndex = BuildIndex(paths);
    }

    private static Dictionary<string, IDataNodePath> BuildIndex(IReadOnlyList<IDataNodePath> paths)
    {
        var index = new Dictionary<string, IDataNodePath>(StringComparer.Ordinal);
        foreach (var p in paths)
        {
            index.TryAdd(p.Name, p);
        }

        return index;
    }

    /// <inheritdoc />
    public IGenericResult<IDataNodePath> Path(string name)
    {
        if (_pathIndex.TryGetValue(name, out var path))
            return GenericResult<IDataNodePath>.Success(path);

        return PathNotFoundResult<IDataNodePath>(name);
    }

    private IGenericResult<T> PathNotFoundResult<T>(string name) =>
        GenericResult<T>.Chain(
            DataStoresResultCodes.DataPathNotFound,
            GenericResult.Failure(DataStoreLoaderLog.PathNotFound(_logger, name, Name)),
            ResultDetails.Create("PathName", name, "DataStoreName", Name));
}
