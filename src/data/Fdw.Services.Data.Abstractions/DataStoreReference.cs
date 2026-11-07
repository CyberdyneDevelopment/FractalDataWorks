using System;

namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// What a caller that only needs to know a data store is there, which connection serves it and whether the host declared it, is given:
/// the store's own row, not the tree of its paths, containers and fields (FDW-4253).
/// </summary>
/// <remarks>
/// Read for a caller, so a store the caller cannot see is not found, exactly as <see cref="IDataStoreProvider.Get(string, System.Threading.CancellationToken)"/>
/// answers it. The tree of a 500-container store is ~2470 requests and 19 s to build (FDW-4252); the row is one cached statement.
/// </remarks>
/// <param name="Name">The store's name.</param>
/// <param name="ConnectionId">The connection that serves the store.</param>
/// <param name="DeclaredByHost">Whether the host declared the store in its configuration schema, which makes it the platform's own.</param>
public sealed record DataStoreReference(string Name, Guid ConnectionId, bool DeclaredByHost);
