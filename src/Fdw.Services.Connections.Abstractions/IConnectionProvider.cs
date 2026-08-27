using Fdw.Abstractions;
using Fdw.ServiceTypes;

using Fdw.Configuration;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Interface for providers that create and manage generic connections.
/// </summary>
public interface IConnectionProvider : IPlatformServiceProvider<IGenericConnection, IConnectionImplementationConfiguration>
{
}
