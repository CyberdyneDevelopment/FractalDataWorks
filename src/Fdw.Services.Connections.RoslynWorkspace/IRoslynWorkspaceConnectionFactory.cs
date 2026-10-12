using Fdw.Results;
using Fdw.Workspace.Roslyn;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.RoslynWorkspace;

/// <summary>
/// Factory interface for creating <see cref="RoslynWorkspaceConnection"/> instances.
/// Registered by <c>RoslynWorkspaceConnectionType</c> in Phase 1 of the
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
    /// <summary>Creates the connection from an already-opened workspace.</summary>
    /// <param name="configuration">The connection configuration.</param>
    /// <param name="workspace">The opened workspace; Live mode requires one, Snapshot ignores it.</param>
    /// <returns>The connection, or a structured failure.</returns>
    /// <remarks>
    /// Declared here and not on <see cref="IConnectionFactory"/>: a workspace means something to this
    /// implementation and nothing to the domain. Opening it is the provider's job, which is what
    /// keeps every Create on this factory synchronous.
    /// </remarks>
    IGenericResult<IGenericConnection> Create(
        RoslynWorkspaceConnectionConfiguration configuration,
        IRoslynWorkspace? workspace);

}
