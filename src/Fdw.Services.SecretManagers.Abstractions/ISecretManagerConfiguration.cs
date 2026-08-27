using Fdw.Configuration;

namespace Fdw.Services.SecretManagers.Abstractions;

/// <summary>
/// One configured secret manager — the domain record, naming which secret-manager implementation it is
/// and holding that implementation's own configuration.
/// </summary>
public interface ISecretManagerConfiguration
    : IPlatformServiceConfiguration<ISecretManagerImplementationConfiguration>
{
}
