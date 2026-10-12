using Fdw.Results;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Data.Abstractions.Discovery;

/// <summary>
/// Resolves an <see cref="ISchemaDiscoverer"/> for a given connection by inspecting
/// its concrete type. Implementations are populated by the connection / data
/// service-type collections at startup so any registered connection type that ships
/// a typed discoverer becomes routable here.
/// </summary>
public interface ISchemaDiscoveryFactory
{
    /// <summary>
    /// Returns the schema discoverer that handles the supplied connection's type.
    /// Failure when no discoverer is registered for the connection's runtime type.
    /// </summary>
    IGenericResult<ISchemaDiscoverer> DiscovererFor(IGenericConnection connection);
}
