using System.Collections.Generic;
using Fdw.Results;

namespace Fdw.Data.Abstractions;

/// <summary>
/// A composite over a set of root <see cref="IDataNode"/>s, exposing uniform synchronous navigation
/// to the roots by name. Specializations close <typeparamref name="TRoot"/> for a concrete root kind
/// (for example <see cref="DataStoreTree"/> over <see cref="IDataStore"/>).
/// </summary>
/// <typeparam name="TRoot">The root node kind held by this tree.</typeparam>
/// <remarks>
/// Why: replaces the <c>RefreshableDataStoreTree : IReadOnlyList&lt;IDataStore&gt;</c> shim with a
/// uniform composite. The tree is navigated synchronously over already-built nodes; from a root, the
/// caller dot-walks the uniform <see cref="IDataNode"/> child surface (<see cref="IDataNode.Nodes"/> /
/// <see cref="IDataNode.Node(string)"/>) or the kind-typed views.
/// </remarks>
public interface IDataNodeTree<out TRoot>
    where TRoot : IDataNode
{
    /// <summary>
    /// Gets the root nodes of this tree.
    /// </summary>
    IReadOnlyList<TRoot> Roots { get; }

    /// <summary>
    /// Returns the root node with the given name, or a failure result if absent.
    /// </summary>
    /// <param name="name">The root node name to look up.</param>
    /// <returns>
    /// Success with the matching root, or Failure when no root with <paramref name="name"/> exists.
    /// Callers MUST check <c>IsSuccess</c> before using <c>.Value</c>.
    /// </returns>
    IGenericResult<TRoot> Node(string name);
}
