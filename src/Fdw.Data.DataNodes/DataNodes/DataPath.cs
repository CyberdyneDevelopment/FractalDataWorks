using System.Collections.Generic;
using Fdw.Data.Abstractions;
using Fdw.Data.Abstractions.Results;
using Fdw.Results;
using Fdw.Services.Data.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.DataNodes;

/// <summary>
/// Runtime implementation of <see cref="IDataNodePath"/>.
/// Eagerly constructed with a container list; <see cref="Container"/> does O(1) dictionary lookup by name.
/// </summary>
internal sealed class DataPath : IDataNodePath
{
    private System.Collections.Generic.Dictionary<string, IDataContainer> _containerIndex;
    private bool _containersFinalized;

    // Why: the node holds a real ILogger so its container-not-found log calls actually emit. Passing
    // NullLogger.Instance to a built log message is a no-op (the message never reaches a sink), so
    // the builder threads its logger in via the constructor. NullLogger fallback is the only ?? allowed.
    private readonly ILogger _logger;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public IDataStore Store { get; }

    /// <inheritdoc />
    public IReadOnlyList<IDataContainer> Containers { get; private set; }

    /// <inheritdoc />
    public string? Description { get; }

    /// <inheritdoc />
    // Why: a path's child nodes ARE its containers — the typed Containers view over the uniform child surface.
    public IReadOnlyList<IDataNode> Nodes => Containers;

    /// <inheritdoc />
    public IGenericResult<IDataNode> Node(string name)
    {
        if (_containerIndex.TryGetValue(name, out var container))
            return GenericResult<IDataNode>.Success(container);

        return ContainerNotFoundResult<IDataNode>(name);
    }

    internal DataPath(string name, IDataStore store, IReadOnlyList<IDataContainer> containers, string? description = null, ILogger? logger = null)
    {
        Name = name;
        Store = store;
        Containers = containers;
        Description = description;
        // Why: NullLogger keeps the node functional without DI logging — the only sanctioned ?? fallback.
        _logger = logger ?? NullLogger.Instance;

        // Why: O(1) lookup dictionary — container names are unique within a path.
        _containerIndex = BuildIndex(containers);
    }

    // Why (chicken-and-egg fix): a container needs its owning path at construction, but the path's
    // container index needs the containers — so the builder constructs the FINAL path first (empty),
    // builds every container under THIS path object, then calls SetContainers to wire the index. Without
    // this, containers were parented to a throwaway empty placeholder path and container.Parent.Container(...)
    // (sibling navigation, e.g. a typed-body JOIN) always missed. Set-once: the index is finalized exactly
    // once (a second call is a wiring defect — fail loud), preserving node immutability after wiring.
    internal void SetContainers(IReadOnlyList<IDataContainer> containers)
    {
        if (containers is null)
            throw new System.ArgumentNullException(nameof(containers));
        if (_containersFinalized)
            throw new System.InvalidOperationException($"DataPath '{Name}' containers already finalized.");

        _containersFinalized = true;
        Containers = containers;
        _containerIndex = BuildIndex(containers);
    }

    private static Dictionary<string, IDataContainer> BuildIndex(IReadOnlyList<IDataContainer> containers)
    {
        var index = new Dictionary<string, IDataContainer>(StringComparer.Ordinal);
        foreach (var c in containers)
        {
            index.TryAdd(c.Name, c);
        }

        return index;
    }

    /// <inheritdoc />
    public IGenericResult<IDataContainer> Container(string name)
    {
        if (_containerIndex.TryGetValue(name, out var container))
            return GenericResult<IDataContainer>.Success(container);

        return ContainerNotFoundResult<IDataContainer>(name);
    }

    // Why: ONE construction point for "this path registers no such container", shared by Node and
    // Container so the two navigation surfaces cannot drift — the mirror of DataStore.PathNotFoundResult.
    // The typed ContainerNotFoundInPath code is CHAINED over the node's own Debug navigation message, so
    // callers branch on Code/CodeChain while CurrentMessage and the Debug log line are unchanged.
    private IGenericResult<T> ContainerNotFoundResult<T>(string name) =>
        GenericResult<T>.Chain(
            DataStoresResultCodes.ContainerNotFoundInPath,
            GenericResult.Failure(DataStoreLoaderLog.ContainerNotFoundInPath(_logger, name, Name)),
            ResultDetails.Create("ContainerName", name, "PathName", Name, "DataStoreName", Store.Name));
}
