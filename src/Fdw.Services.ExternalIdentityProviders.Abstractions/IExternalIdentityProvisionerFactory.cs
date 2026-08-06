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
// Why: TConfiguration is constrained to `class` because IFdwServiceProvider<TService, TConfiguration>
// requires a reference type, and the provider-supplied Create overload below names that interface.
public interface IExternalIdentityProvisionerFactory<TService, TConfiguration> : IExternalIdentityProvisionerFactory, IServiceFactory<TService, TConfiguration>
    where TService : IExternalIdentityProvisioner
    where TConfiguration : class, IGenericConfiguration
{
    /// <summary>
    /// Creates a provisioner, receiving the already-resolved provisioner provider it may use to look
    /// up sibling provisioners by name at <c>Provision</c> time. The factory is a PURE construction
    /// over the supplied values and resolves nothing itself.
    /// </summary>
    /// <param name="configuration">The composed provisioner configuration header.</param>
    /// <param name="provisionerProvider">
    /// The resolved provisioner provider — supplied by the provider itself (<c>this</c>), never
    /// resolved from the container by the factory.
    /// </param>
    // Why: mirrors IDataVaultFactory.Create(config, connection, pepper). A factory that ctor-injected
    // this provider re-entered the provider's own resolver lambda and recursed until the host was
    // killed (FDW-615). Passing it as a plain argument from the provider makes that impossible —
    // there is no container lookup involved at all.
    IGenericResult<TService> Create(TConfiguration configuration, IFdwServiceProvider<TService, TConfiguration> provisionerProvider);
}
