using Fdw.Services.Abstractions;

namespace Fdw.Services.SecretManagers.Abstractions;

/// <summary>
/// Resolves configured secret managers and routes each to the implementation provider that owns it.
/// </summary>
public interface ISecretManagerConfigurationProvider
    : IDomainConfigurationProvider<ISecretManagerImplementationConfiguration>
{
}
