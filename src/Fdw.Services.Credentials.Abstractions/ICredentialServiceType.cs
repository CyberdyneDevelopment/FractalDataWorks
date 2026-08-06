using Fdw.Configuration;
using Fdw.ServiceTypes;

namespace Fdw.Services.Credentials.Abstractions;

/// <summary>
/// Marker interface for credential service type definitions.
/// </summary>
public interface ICredentialServiceType : IServiceType
{
}

/// <summary>
/// Generic interface for credential service type definitions with typed parameters.
/// </summary>
/// <typeparam name="TService">The credential service type.</typeparam>
/// <typeparam name="TFactory">The factory type for creating credential service instances.</typeparam>
/// <typeparam name="TConfiguration">The configuration type for the service.</typeparam>
public interface ICredentialServiceType<TService, TFactory, TConfiguration>
    : ICredentialServiceType, IServiceType<System.Guid, TService, TFactory, TConfiguration>
    where TService : ICredentialService
    where TConfiguration : IGenericConfiguration
    where TFactory : ICredentialServiceFactory<TService, TConfiguration>
{
}
