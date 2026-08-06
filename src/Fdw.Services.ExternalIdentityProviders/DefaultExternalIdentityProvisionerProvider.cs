using Fdw.Abstractions;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders;

/// <summary>
/// Provisioner domain provider. Supplies itself to the registered factory so a built provisioner can
/// look up sibling provisioners by name at <c>Provision</c> time.
/// </summary>
/// <remarks>
/// <para>
/// This exists to keep provisioner factories PURE. A factory that ctor-injected
/// <c>IFdwServiceProvider&lt;IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration&gt;</c>
/// was resolved from inside that provider's own generated scoped resolver lambda, so resolving it
/// re-entered the lambda — whose cache entry is not published yet — and recursed without bound. MEDI's
/// StackGuard migrates that recursion onto fresh stacks instead of throwing, so the host hung SILENTLY
/// until it was killed (FDW-615).
/// </para>
/// <para>
/// Overriding <see cref="DefaultServiceProvider{TService,TConfiguration,TFactory,TConfigurationProvider}.Create"/>
/// removes the container from the picture entirely: the provider passes <c>this</c> — a value it
/// already holds — as a plain method argument. Mirrors <c>DefaultDataVaultProvider</c>, which hands the
/// factory an already-resolved connection and pepper.
/// </para>
/// </remarks>
public class DefaultExternalIdentityProvisionerProvider
    : DefaultServiceProvider<
        IExternalIdentityProvisioner,
        ExternalIdentityProvisionerConfiguration,
        IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration>,
        IServiceConfigurationProvider<ExternalIdentityProvisionerConfiguration>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultExternalIdentityProvisionerProvider"/> class.
    /// </summary>
    /// <param name="services">The container this provider resolves factories from.</param>
    /// <param name="logger">Logger instance.</param>
    public DefaultExternalIdentityProvisionerProvider(
        IServiceProvider services,
        ILogger<DefaultServiceProvider<
            IExternalIdentityProvisioner,
            ExternalIdentityProvisionerConfiguration,
            IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration>,
            IServiceConfigurationProvider<ExternalIdentityProvisionerConfiguration>>> logger)
        : base(services, logger ?? NullLogger<DefaultServiceProvider<
            IExternalIdentityProvisioner,
            ExternalIdentityProvisionerConfiguration,
            IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration>,
            IServiceConfigurationProvider<ExternalIdentityProvisionerConfiguration>>>.Instance)
    {
    }

    /// <inheritdoc />
    // Why: hand the factory the provider it needs for Provision-time sibling lookup. `this` is a value
    // we already hold — no container resolution, so the FDW-615 re-entrancy cannot occur. A factory that
    // does not implement the domain overload still works via the base pure-construction path.
    protected override IGenericResult<IExternalIdentityProvisioner> Create(
        IServiceFactory<IExternalIdentityProvisioner> factory,
        ExternalIdentityProvisionerConfiguration configuration)
    {
        if (factory is IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration> provisionerFactory)
            return provisionerFactory.Create(configuration, this);

        return base.Create(factory, configuration);
    }
}
