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
/// A chained provisioner needs the provider to resolve its sibling steps at Provision time. It is
/// given that provider when its factory is constructed, so nothing resolves it from the container at
/// create time — which is what recursed without bound when it did (FDW-615).
/// </remarks>
public class ExternalIdentityProvisionerServiceProvider
    : PlatformServiceProviderBase<
        IExternalIdentityProvisioner,
        IExternalIdentityProvisionerImplementationConfiguration,
        IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>,
        IExternalIdentityProvisionerConfigurationProvider>,
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
            IExternalIdentityProvisionerImplementationConfiguration,
            IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>,
            IExternalIdentityProvisionerConfigurationProvider>> logger)
        : base(services, logger ?? NullLogger<PlatformServiceProviderBase<
            IExternalIdentityProvisioner,
            IExternalIdentityProvisionerImplementationConfiguration,
            IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>,
            IExternalIdentityProvisionerConfigurationProvider>>.Instance)
    {
    }
}
