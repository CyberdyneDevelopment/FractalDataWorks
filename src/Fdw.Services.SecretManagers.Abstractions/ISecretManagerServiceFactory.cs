using Fdw.Abstractions;
using Fdw.Configuration;

namespace Fdw.Services.SecretManagers.Abstractions;

/// <summary>
/// Marker interface for secret manager factories.
/// </summary>
public interface ISecretManagerServiceFactory
{
}

/// <summary>
/// Generic interface for secret manager factories with typed configuration.
/// </summary>
/// <typeparam name="TSecretService">The secret management service type to create.</typeparam>
/// <typeparam name="TConfiguration">The configuration type for the secret management service.</typeparam>
public interface ISecretManagerServiceFactory<TSecretService, TConfiguration> : ISecretManagerServiceFactory, IServiceFactory<TSecretService, TConfiguration>
    where TSecretService : ISecretManager
    where TConfiguration : IGenericConfiguration
{
}
