using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.FileSystem;

/// <summary>
/// Factory interface for creating <see cref="FileSystemConnection"/> instances.
/// Registered by <see cref="FileSystemConnectionType"/> in Phase 1 of the
/// ServiceTypeCollection three-phase lifecycle.
/// </summary>
/// <remarks>
/// Why <c>IGenericConnection</c> as TConnection: all connection factories use the non-generic
/// base type so the returned result is compatible with the generic <c>IConnectionFactory</c>
/// contract (mirrors <c>IPostgreSqlConnectionFactory</c> and <c>IMsSqlConnectionFactory</c>).
/// Callers that need the typed <c>IFileSystemConnection</c> cast the result.
/// </remarks>
public interface IFileSystemConnectionFactory : IConnectionFactory<IGenericConnection, FileSystemConnectionConfiguration>
{
}
