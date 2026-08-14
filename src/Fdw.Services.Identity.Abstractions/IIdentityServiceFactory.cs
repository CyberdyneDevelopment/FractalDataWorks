using Fdw.Abstractions;
using Fdw.Configuration;

namespace Fdw.Services.Identity.Abstractions;

/// <summary>
/// Marker interface for identity service factories. The non-generic marker lets consumers reference
/// "an identity service factory" without knowing TService/TConfiguration; the generic form is what
/// concrete factories (and the closed <c>IdentityServiceTypes</c> ServiceTypeCollection) close over.
/// </summary>
public interface IIdentityServiceFactory
{
}

/// <summary>
/// Generic interface for identity service factories with typed configuration.
/// </summary>
/// <typeparam name="TService">The type of identity service this factory creates.</typeparam>
/// <typeparam name="TConfiguration">The type of configuration this factory requires.</typeparam>
public interface IIdentityServiceFactory<TService, TConfiguration> : IIdentityServiceFactory, IServiceFactory<TService, TConfiguration>
    where TService : IIdentityService
    where TConfiguration : IGenericConfiguration
{
}
