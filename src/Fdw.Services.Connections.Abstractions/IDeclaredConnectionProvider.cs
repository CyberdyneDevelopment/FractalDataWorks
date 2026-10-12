using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Builds a connection the host declares in its <c>configurationSchema.json</c>.
/// </summary>
/// <remarks>
/// A declared connection is the platform's own: no ConfigurationDb row names it, so
/// <see cref="IDataConnectionProvider"/> never returns it and no route a user can call reaches it. Platform plumbing
/// that needs one by name -- the credential vault over AuthDb -- resolves it here. A name the host does not declare
/// is a refusal with the reason, never a lookup elsewhere.
/// </remarks>
public interface IDeclaredConnectionProvider
{
    /// <summary>Builds the declared connection named <paramref name="connectionName"/>.</summary>
    /// <param name="connectionName">The connection's name in <c>configurationSchema.json</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The connection, or the failure that says why it could not be built.</returns>
    Task<IGenericResult<IDataConnection>> Get(string connectionName, CancellationToken cancellationToken = default);
}
