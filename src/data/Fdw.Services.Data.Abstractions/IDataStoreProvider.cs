using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Resolves DataStores as the uniform <see cref="IDataNode"/> tree. <see cref="Get(string,CancellationToken)"/>
/// returns a fully-built, navigable <see cref="IDataStore"/> (Paths → Containers, assembled once by the
/// per-transport <c>IDataStoreBuilder</c>); container fields are the lazy leaf, resolved when a container
/// is actually used. Path and container lookups are <c>Get</c> OVERLOADS that dot-walk the built tree
/// (<c>store.Path(name).Container(name)</c>) — there is no separate <c>GetContainer</c> verb.
/// </summary>
public interface IDataStoreProvider
{
    /// <summary>
    /// Gets the store's own row: its name, the connection that serves it and whether the host declared it, without building the tree of its
    /// paths, containers and fields (FDW-4253).
    /// </summary>
    /// <param name="name">The store's name.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The reference, or the failure <see cref="Get(string, CancellationToken)"/> gives for a store that is not there or that the caller cannot see.</returns>
    /// <remarks>
    /// For a caller that only asks whether a store is there, or reads <c>ConnectionId</c> or <c>DeclaredByHost</c> of it. A caller that walks the
    /// tree uses <see cref="Get(string, CancellationToken)"/>.
    /// </remarks>
    Task<IGenericResult<DataStoreReference>> GetReference(string name, CancellationToken cancellationToken = default);

    /// <summary>Gets a DataStore by name as the navigable node tree (Paths + Containers).</summary>
    Task<IGenericResult<IDataStore>> Get(string name, CancellationToken cancellationToken = default);

    /// <summary>Gets a DataStore by its durable identifier as the navigable node tree.</summary>
    Task<IGenericResult<IDataStore>> Get(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets all DataStores.</summary>
    Task<IGenericResult<IReadOnlyList<IDataStore>>> Get(CancellationToken cancellationToken = default);

    /// <summary>Gets a path within a DataStore (dot-walk of <c>Get(store).Path(path)</c>).</summary>
    Task<IGenericResult<IDataNodePath>> Get(string dataStoreName, string pathName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a container within a path of a DataStore — the whole container with its fields
    /// (dot-walk of <c>Get(store).Path(path).Container(container)</c>).
    /// </summary>
    Task<IGenericResult<IDataContainer>> Get(string dataStoreName, string pathName, string containerName, CancellationToken cancellationToken = default);
}
