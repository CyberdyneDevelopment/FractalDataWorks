using Fdw.Abstractions;
using Fdw.Configuration;

namespace Fdw.Services.Credentials.Abstractions;

/// <summary>
/// Marker interface for credential service factories.
/// </summary>
public interface ICredentialServiceFactory
{
}

/// <summary>
/// Generic interface for credential service factories with typed configuration.
/// </summary>
/// <typeparam name="TService">The credential service type to create.</typeparam>
/// <typeparam name="TConfiguration">The configuration type for the service.</typeparam>
public interface ICredentialServiceFactory<TService, TConfiguration> : ICredentialServiceFactory, IServiceFactory<TService, TConfiguration>
    where TService : ICredentialService
    where TConfiguration : IGenericConfiguration
{
}
