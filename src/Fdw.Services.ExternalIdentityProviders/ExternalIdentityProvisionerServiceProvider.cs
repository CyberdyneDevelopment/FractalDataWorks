using Fdw.Abstractions;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Fdw.Configuration;

namespace Fdw.Services.ExternalIdentityProviders;

/// <summary>
/// Provisioner domain provider. Supplies itself to the registered factory so a built provisioner can
/// look up sibling provisioners by name at <c>Provision</c> time.
/// </summary>
/// <remarks>
/// <para>
/// This exists to keep provisioner factories PURE. A factory that ctor-injected
/// <c>IPlatformServiceProvider&lt;IExternalIdentityProvisioner, IExternalIdentityProvisionerConfiguration&gt;</c>
/// was resolved from inside that provider's own generated scoped resolver lambda, so resolving it
/// re-entered the lambda — whose cache entry is not published yet — and recursed without bound. MEDI's
/// StackGuard migrates that recursion onto fresh stacks instead of throwing, so the host hung SILENTLY
/// until it was killed (FDW-615).
/// </para>
/// <para>
/// Overriding <see cref="PlatformServiceProviderBase{TService,TConfiguration,TFactory,TConfigurationProvider}.Create"/>
/// removes the container from the picture entirely: the provider passes <c>this</c> — a value it
/// already holds — as a plain method argument. Mirrors <c>DataVaultProvider</c>, which hands the
/// factory an already-resolved connection and pepper.
/// </para>
/// </remarks>
public class ExternalIdentityProvisionerServiceProvider
    : PlatformServiceProviderBase<
        IExternalIdentityProvisioner,
        IExternalIdentityProvisionerConfiguration,
        IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, IExternalIdentityProvisionerConfiguration>,
        IServiceConfigurationProvider<IExternalIdentityProvisionerConfiguration>>,
      IExternalIdentityProvisionerServiceProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalIdentityProvisionerServiceProvider"/> class.
    /// </summary>
    /// <param name="services">The container this provider resolves factories from.</param>
    /// <param name="logger">Logger instance.</param>
    public ExternalIdentityProvisionerServiceProvider(
        IServiceProvider services,
        ILogger<PlatformServiceProviderBase<
            IExternalIdentityProvisioner,
            IExternalIdentityProvisionerConfiguration,
            IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, IExternalIdentityProvisionerConfiguration>,
            IServiceConfigurationProvider<IExternalIdentityProvisionerConfiguration>>> logger)
        : base(services, logger ?? NullLogger<PlatformServiceProviderBase<
            IExternalIdentityProvisioner,
            IExternalIdentityProvisionerConfiguration,
            IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, IExternalIdentityProvisionerConfiguration>,
            IServiceConfigurationProvider<IExternalIdentityProvisionerConfiguration>>>.Instance)
    {
    }
}
