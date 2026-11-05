using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections;

/// <summary>A store an assignment led to, with the connection and folder it was narrowed to, for the log line that reports it (FDW-3396).</summary>
/// <param name="Store">The narrowed store.</param>
/// <param name="ConnectionName">The granted connection.</param>
/// <param name="Root">The folder the store is narrowed to.</param>
internal sealed record OpenedStore(IDocumentStore Store, string ConnectionName, string Root);
