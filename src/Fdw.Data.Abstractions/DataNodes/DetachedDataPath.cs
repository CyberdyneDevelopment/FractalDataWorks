using System;
using System.Collections.Generic;
using Fdw.Data.Abstractions.Logging;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.Abstractions;

/// <summary>
/// A minimal <see cref="IDataNodePath"/> for a detached container reached by direct physical address
/// rather than tree navigation. It is the structurally-valid (but empty) tree parent for a
/// <see cref="DataContainer"/> built outside the DataStore→Path→Container tree.
/// </summary>
/// <remarks>
/// Why: see <see cref="DetachedDataStore"/>. The detached path exposes no child containers — the
/// container that owns it as its <see cref="IDataContainer.Parent"/> is reached by its physical
/// <see cref="IStorageContainer.Path"/>, never by walking back up the tree.
/// </remarks>
public sealed class DetachedDataPath : IDataNodePath
{
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="DetachedDataPath"/> class.</summary>
    /// <param name="name">The path name (typically the schema name).</param>
    /// <param name="store">The owning detached store.</param>
    /// <param name="logger">Logger for navigation diagnostics. Defaults to a null logger.</param>
    public DetachedDataPath(string name, IDataStore store, ILogger? logger = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Store = store ?? throw new ArgumentNullException(nameof(store));
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string? Description => null;

    /// <inheritdoc />
    public IDataStore Store { get; }

    /// <inheritdoc />
    public IReadOnlyList<IDataContainer> Containers => [];

    /// <inheritdoc />
    public IReadOnlyList<IDataNode> Nodes => [];

    /// <inheritdoc />
    public IGenericResult<IDataContainer> Container(string name)
        => GenericResult<IDataContainer>.Failure(DataNodeTreeLog.RootNodeNotFound(_logger, name));

    /// <inheritdoc />
    public IGenericResult<IDataNode> Node(string name)
        => GenericResult<IDataNode>.Failure(DataNodeTreeLog.RootNodeNotFound(_logger, name));
}
