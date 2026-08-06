using Fdw.Results;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Data.Abstractions.Discovery;

/// <summary>
/// Resolves an <see cref="ISchemaDiscoverer"/> for a given connection by inspecting
/// its concrete type. Implementations are populated by the connection / data
/// service-type collections at startup so any registered connection type that ships
/// a typed discoverer becomes routable here.
/// </summary>
// Why: The CLI's `discover datastore` verb (and the web UI's schema-import flow)
// shouldn't know whether a connection is MsSql, PostgreSql, or Http. They hand the
// connection to the factory and get back the right discoverer.
public interface ISchemaDiscoveryFactory
{
    /// <summary>
    /// Returns the schema discoverer that handles the supplied connection's type.
    /// Failure when no discoverer is registered for the connection's runtime type.
    /// </summary>
    IGenericResult<ISchemaDiscoverer> DiscovererFor(IGenericConnection connection);
}
