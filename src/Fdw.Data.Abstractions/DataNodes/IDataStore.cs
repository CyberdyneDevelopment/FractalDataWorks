using System;
using System.Collections.Generic;
using Fdw.Results;

namespace Fdw.Data.Abstractions;

/// <summary>
/// A named data store that aggregates one or more <see cref="IDataPath"/> scopes,
/// each containing <see cref="IDataContainer"/> instances.
/// </summary>
/// <remarks>
/// A store is the root of a uniform <see cref="IDataNode"/> tree: its child <see cref="IDataNode.Nodes"/>
/// are its <see cref="Paths"/>. The tree is built complete by the per-transport builder and navigated
/// synchronously; there is no eager/lazy split.
/// </remarks>
public interface IDataStore : IDataNode
{
    /// <summary>
    /// Gets the identifier of the connection that provides physical access to this store.
    /// </summary>
    Guid ConnectionId { get; }

    /// <summary>
    /// Gets all paths (schemas, directories, namespaces) within this store.
    /// </summary>
    /// <remarks>
    /// This is the typed view of <see cref="IDataNode.Nodes"/> — every element is also a child node.
    /// </remarks>
    IReadOnlyList<IDataPath> Paths { get; }

    /// <summary>
    /// Returns the path with the given name, or a failure result if absent.
    /// </summary>
    /// <param name="name">The path name to look up.</param>
    /// <returns>
    /// Success with the matching <see cref="IDataPath"/>, or Failure when no path with
    /// <paramref name="name"/> exists. Callers MUST check <c>IsSuccess</c> before using <c>.Value</c>.
    /// </returns>
    IGenericResult<IDataPath> Path(string name);
}
