using Fdw.Abstractions;
using Fdw.Configuration;

namespace Fdw.Services.ExternalIdentityProviders.Abstractions;

/// <summary>
/// Marker interface for external identity provider factories. Mirrors <c>ITokenManagerFactory</c>:
/// the non-generic marker lets consumers reference "an external identity provider factory" without
/// knowing TService/TConfiguration; the generic form below is what concrete factories (and the closed
/// <c>ExternalIdentityProviderTypes</c> ServiceTypeCollection) actually implement/close over.
/// </summary>
public interface IExternalIdentityProviderFactory
{
}

/// <summary>
/// Generic interface for external identity provider factories with typed configuration.
/// </summary>
/// <typeparam name="TService">The type of external identity provider this factory creates.</typeparam>
/// <typeparam name="TConfiguration">The type of configuration this factory requires.</typeparam>
public interface IExternalIdentityProviderFactory<TService, TConfiguration> : IExternalIdentityProviderFactory, IServiceFactory<TService, TConfiguration>
    where TService : IExternalIdentityProvider
    where TConfiguration : IGenericConfiguration
{
}
