using System;
using System.Collections.Generic;
using Fdw.Results;

namespace Fdw.Data.Abstractions;

/// <summary>
/// A named data store that aggregates one or more <see cref="IDataNodePath"/> scopes,
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
    /// Gets whether the host declared this store in its <c>configurationSchema.json</c> for the platform's own use,
    /// rather than it being a store configured at run time.
    /// </summary>
    /// <remarks>
    /// Set by the configuration gateway, which builds exactly the stores the host declares, and by nothing else. A
    /// host-declared store holds the platform's own configuration, credentials and sessions, so the main data gateway
    /// refuses to run a request over one unless the caller is the system context (FDW-1068).
    /// </remarks>
    bool DeclaredByHost { get; }

    /// <summary>
    /// Gets all paths (schemas, directories, namespaces) within this store.
    /// </summary>
    /// <remarks>
    /// This is the typed view of <see cref="IDataNode.Nodes"/> — every element is also a child node.
    /// </remarks>
    IReadOnlyList<IDataNodePath> Paths { get; }

    /// <summary>
    /// Returns the path with the given name, or a failure result if absent.
    /// </summary>
    /// <param name="name">The path name to look up.</param>
    /// <returns>
    /// Success with the matching <see cref="IDataNodePath"/>, or Failure when no path with
    /// <paramref name="name"/> exists. Callers MUST check <c>IsSuccess</c> before using <c>.Value</c>.
    /// </returns>
    IGenericResult<IDataNodePath> Path(string name);
}
