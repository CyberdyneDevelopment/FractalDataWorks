using Fdw.ServiceTypes;

namespace Fdw.Services.SecretManagers.Abstractions;

/// <summary>
/// Provider interface for secret manager services.
/// Replaces the generic <see cref="IPlatformServiceProvider"/> usage with a proper domain-specific interface,
/// eliminating the need for casting when resolving secret managers.
/// </summary>
public interface ISecretManagerProvider : IPlatformServiceProvider<ISecretManager, ISecretManagerImplementationConfiguration>
{
}
