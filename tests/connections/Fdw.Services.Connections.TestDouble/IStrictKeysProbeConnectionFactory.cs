using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.TestDouble;

/// <summary>
/// Factory contract for <see cref="StrictKeysProbeConnectionConfiguration"/> (FDW-6584): <c>ConnectionTypeBase</c> is generic over a factory typed for its own configuration, and nothing opens a connection through this one.
/// </summary>
public interface IStrictKeysProbeConnectionFactory : IConnectionFactory<IGenericConnection, StrictKeysProbeConnectionConfiguration>
{
}
