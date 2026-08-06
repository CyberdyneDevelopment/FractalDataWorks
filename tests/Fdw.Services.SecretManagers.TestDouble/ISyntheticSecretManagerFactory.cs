using Fdw.Services.SecretManagers.Abstractions;

namespace Fdw.Services.SecretManagers.TestDouble;

/// <summary>
/// Factory contract for the <c>Synthetic</c> secret manager, mirroring the shape every shipped
/// backend uses (<c>IEnvironmentVariableSecretManagerFactory</c> et al).
/// </summary>
public interface ISyntheticSecretManagerFactory
    : ISecretManagerServiceFactory<ISecretManager, SyntheticSecretManagerConfiguration>
{
}
