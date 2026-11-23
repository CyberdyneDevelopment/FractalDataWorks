using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.TestDouble;

/// <summary>
/// Factory contract for <see cref="DdlProbeConnectionConfiguration"/> (FDW-3617): <c>ConnectionTypeBase</c> is generic over a factory typed for its own configuration, and nothing opens a connection through this one.
/// </summary>
public interface IDdlProbeConnectionFactory : IConnectionFactory<IGenericConnection, DdlProbeConnectionConfiguration>
{
}
