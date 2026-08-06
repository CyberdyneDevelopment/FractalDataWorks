using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Fdw.Data.Abstractions;

/// <summary>
/// The root-is-<see cref="IDataStore"/> specialization of <see cref="DataNodeTree{TRoot}"/>.
/// Holds the set of registered data stores and resolves them by name.
/// </summary>
/// <remarks>
/// Why: replaces the <c>RefreshableDataStoreTree : IReadOnlyList&lt;IDataStore&gt;</c> shim. Callers
/// resolve a store via <see cref="DataNodeTree{TRoot}.Node(string)"/> and then dot-walk
/// <see cref="IDataStore.Paths"/> / <see cref="IDataPath.Containers"/> / fields.
/// </remarks>
// Why: pure data holder, no logic beyond trivial construction/assignment
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class DataStoreTree : DataNodeTree<IDataStore>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataStoreTree"/> class.
    /// </summary>
    /// <param name="stores">The data stores that form the roots of this tree.</param>
    /// <param name="logger">Logger for navigation diagnostics. Defaults to a null logger.</param>
    public DataStoreTree(IReadOnlyList<IDataStore> stores, ILogger? logger = null)
        : base(stores, logger)
    {
    }
}
