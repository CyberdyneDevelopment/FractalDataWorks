namespace Fdw.Services.Data.Abstractions;

/// <summary>
/// Identifies a container within a specific DataStore, addressed by store name, optional
/// path, and container name. Used with the target-typed overloads of <see cref="IDataGateway"/>.
/// </summary>
/// <param name="DataStore">The logical DataStore name.</param>
/// <param name="Path">
/// The optional path (schema) within the DataStore. When <see langword="null"/>, all paths
/// in the store are searched — preserving the "search all paths" behaviour of the
/// command-only route.
/// </param>
/// <param name="Container">The container (table) name.</param>
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed record DataStoreTarget(string DataStore, string? Path, string Container);
