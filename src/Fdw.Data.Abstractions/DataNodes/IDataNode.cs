using System.Collections.Generic;
using Fdw.Results;

namespace Fdw.Data.Abstractions;

/// <summary>
/// The uniform base for every node in a data-store tree: <see cref="IDataStore"/>,
/// <see cref="IDataNodePath"/>, <see cref="IDataContainer"/>, and <see cref="IDataField"/>.
/// </summary>
/// <remarks>
/// Every node carries a <see cref="Name"/>, an optional <see cref="Description"/>, and a uniform
/// synchronous child navigation surface (<see cref="Nodes"/> + <see cref="Node(string)"/>). The
/// typed views each kind exposes (<see cref="IDataStore.Paths"/>, <see cref="IDataNodePath.Containers"/>,
/// a container's fields) are the same child set under a kind-specific element type.
/// <para>
/// Why: the tree is built complete and navigated synchronously. The former asynchronous
/// <c>GetFields</c> and the node-level <c>Keys</c> are gone from this contract — fields are the
/// child <see cref="IDataField"/> nodes of an <see cref="IDataContainer"/> (resolved at build time),
/// and <c>Keys</c> belong on <see cref="IDataContainer"/> where they are meaningful. There is no
/// sync-over-async <c>Lazy.GetAwaiter().GetResult()</c> anywhere on the node tree.
/// </para>
/// </remarks>
public interface IDataNode
{
    /// <summary>
    /// Gets the unique name of this node within its parent scope.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets an optional human-readable description of this node.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets this node's child nodes. Empty for leaf nodes (for example <see cref="IDataField"/>).
    /// </summary>
    /// <remarks>
    /// The children are the same set each kind exposes through its typed view: a store's children
    /// are its paths, a path's children are its containers, a container's children are its fields.
    /// </remarks>
    IReadOnlyList<IDataNode> Nodes { get; }

    /// <summary>
    /// Returns the child node with the given name, or a failure result if absent.
    /// </summary>
    /// <param name="name">The child node name to look up.</param>
    /// <returns>
    /// Success with the matching <see cref="IDataNode"/>, or Failure when no child with
    /// <paramref name="name"/> exists. Callers MUST check <c>IsSuccess</c> before using <c>.Value</c>.
    /// </returns>
    IGenericResult<IDataNode> Node(string name);
}
