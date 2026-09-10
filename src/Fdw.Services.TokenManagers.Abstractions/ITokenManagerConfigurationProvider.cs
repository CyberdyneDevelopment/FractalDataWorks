using Fdw.Services.Abstractions;

namespace Fdw.Services.TokenManagers.Abstractions;

/// <summary>
/// Resolves configured token managers and routes each to the implementation provider that owns it.
/// </summary>
public interface ITokenManagerConfigurationProvider
    : IDomainConfigurationProvider<ITokenManagerImplementationConfiguration>
{
}
