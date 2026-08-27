using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.ServiceTypes;

namespace Fdw.Services.ExternalIdentityProviders.Abstractions;

/// <summary>
/// Marker interface for external identity provisioner factories. Mirrors
/// <c>IExternalIdentityProviderFactory</c>: the non-generic marker lets consumers reference "an
/// external identity provisioner factory" without knowing TService/TConfiguration; the generic form
/// below is what concrete factories (and the closed <c>ExternalIdentityProvisionerTypes</c>
/// ServiceTypeCollection) actually implement/close over.
/// </summary>
public interface IExternalIdentityProvisionerFactory
{
}

/// <summary>
/// Generic interface for external identity provisioner factories with typed configuration.
/// </summary>
/// <typeparam name="TService">The type of external identity provisioner this factory creates.</typeparam>
/// <typeparam name="TConfiguration">The type of configuration this factory requires.</typeparam>
// Why: TConfiguration is constrained to `class` because IPlatformServiceProvider<TService, TConfiguration>
// requires a reference type, and the provider-supplied Create overload below names that interface.
public interface IExternalIdentityProvisionerFactory<TService, TConfiguration> : IExternalIdentityProvisionerFactory, IServiceFactory<TService, TConfiguration>
    where TService : IExternalIdentityProvisioner
    where TConfiguration : class, IGenericConfiguration
{
}
