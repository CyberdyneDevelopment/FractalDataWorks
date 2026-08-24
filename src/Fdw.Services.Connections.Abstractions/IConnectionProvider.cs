using Fdw.Abstractions;
using Fdw.ServiceTypes;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Interface for providers that create and manage generic connections.
/// </summary>
// Why: Connection providers provide CONNECTIONS, not configurations.
// Configuration comes from ConnectionConfigurationProvider.
public interface IConnectionProvider : IPlatformServiceProvider<IGenericConnection>
{
}
