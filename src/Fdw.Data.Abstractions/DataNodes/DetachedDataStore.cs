using System;
using System.Collections.Generic;
using Fdw.Data.Abstractions.Logging;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.Abstractions;

/// <summary>
/// A minimal <see cref="IDataStore"/> for a detached container that is reached by direct physical
/// address (a one-off SQL query container, for example) rather than by tree navigation.
/// </summary>
/// <remarks>
/// Why: the unified Execute seam takes <see cref="IDataContainer"/>, which requires a tree
/// <see cref="IDataContainer.Parent"/>. Some containers — direct-address SQL query containers built
/// by a service handler — never participate in the DataStore→Path→Container tree, yet must still be
/// valid <see cref="IDataContainer"/> instances. This detached store/path pair supplies a structurally
/// valid (but empty) tree parent so such a container satisfies the contract without a synthetic full
/// tree. It carries <see cref="ConnectionId"/> = <see cref="Guid.Empty"/> and no child paths.
/// </remarks>
public sealed class DetachedDataStore : IDataStore
{
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="DetachedDataStore"/> class.</summary>
    /// <param name="name">The store name (typically the database/connection name).</param>
    /// <param name="logger">Logger for navigation diagnostics. Defaults to a null logger.</param>
    public DetachedDataStore(string name, ILogger? logger = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string? Description => null;

    /// <inheritdoc />
    public Guid ConnectionId => Guid.Empty;

    /// <inheritdoc />
    public IReadOnlyList<IDataNodePath> Paths => [];

    /// <inheritdoc />
    public IReadOnlyList<IDataNode> Nodes => [];

    /// <inheritdoc />
    public IGenericResult<IDataNodePath> Path(string name)
        => GenericResult<IDataNodePath>.Failure(DataNodeTreeLog.RootNodeNotFound(_logger, name));

    /// <inheritdoc />
    public IGenericResult<IDataNode> Node(string name)
        => GenericResult<IDataNode>.Failure(DataNodeTreeLog.RootNodeNotFound(_logger, name));
}
