using System;
using System.Collections.Generic;
using Fdw.Data.Abstractions.Logging;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Concrete generic composite over a set of root <see cref="IDataNode"/>s. Indexes the roots by name
/// for O(1) <see cref="Node(string)"/> lookup. Specializations close <typeparamref name="TRoot"/>
/// (for example <see cref="DataStoreTree"/>).
/// </summary>
/// <typeparam name="TRoot">The root node kind held by this tree.</typeparam>
public class DataNodeTree<TRoot> : IDataNodeTree<TRoot>
    where TRoot : IDataNode
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, TRoot> _index;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataNodeTree{TRoot}"/> class.
    /// </summary>
    /// <param name="roots">The root nodes of this tree.</param>
    /// <param name="logger">Logger for navigation diagnostics. Defaults to <see cref="NullLogger.Instance"/>.</param>
    public DataNodeTree(IReadOnlyList<TRoot> roots, ILogger? logger = null)
    {
        Roots = roots ?? throw new ArgumentNullException(nameof(roots));
        // Why: NullLogger keeps the tree functional when DI logging is not wired — the only sanctioned ?? fallback.
        _logger = logger ?? NullLogger.Instance;

        // Why: O(1) lookup dictionary — root node names are unique within a tree.
        _index = new Dictionary<string, TRoot>(StringComparer.Ordinal);
        for (var i = 0; i < roots.Count; i++)
        {
            _index[roots[i].Name] = roots[i];
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<TRoot> Roots { get; }

    /// <inheritdoc />
    public IGenericResult<TRoot> Node(string name)
    {
        if (_index.TryGetValue(name, out var root))
            return GenericResult<TRoot>.Success(root);

        return GenericResult<TRoot>.Failure(DataNodeTreeLog.RootNodeNotFound(_logger, name));
    }
}
