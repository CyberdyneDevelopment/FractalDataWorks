using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.RoslynWorkspace;

/// <summary>
/// Factory interface for creating <see cref="RoslynWorkspaceConnection"/> instances.
/// Registered by <see cref="RoslynWorkspaceConnectionType"/> in Phase 1 of the
/// ServiceTypeCollection three-phase lifecycle.
/// </summary>
/// <remarks>
/// Why <c>IGenericConnection</c> as TConnection: all connection factories use the non-generic
/// base type so the returned result is compatible with the generic <c>IConnectionFactory</c>
/// contract (mirrors <c>IFileSystemConnectionFactory</c> and <c>IMsSqlConnectionFactory</c>).
/// Callers that need the typed <c>IRoslynWorkspaceConnection</c> cast the result.
/// </remarks>
public interface IRoslynWorkspaceConnectionFactory : IConnectionFactory<IGenericConnection, RoslynWorkspaceConnectionConfiguration>
{
}
