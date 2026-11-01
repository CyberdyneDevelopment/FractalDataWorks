using Fdw.Services.Abstractions;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Resolves configured connections and routes each to the implementation provider that owns it.
/// </summary>
public interface IConnectionConfigurationProvider
    : IDomainConfigurationProvider<IConnectionImplementationConfiguration>
{
}
