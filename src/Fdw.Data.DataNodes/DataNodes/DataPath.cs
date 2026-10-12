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
        _logger = logger ?? NullLogger.Instance;

        _containerIndex = BuildIndex(containers);
    }

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

    private IGenericResult<T> ContainerNotFoundResult<T>(string name) =>
        GenericResult<T>.Chain(
            DataStoresResultCodes.ContainerNotFoundInPath,
            GenericResult.Failure(DataStoreLoaderLog.ContainerNotFoundInPath(_logger, name, Name)),
            ResultDetails.Create("ContainerName", name, "PathName", Name, "DataStoreName", Store.Name));
}
