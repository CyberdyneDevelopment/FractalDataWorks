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
/// A provisioner that needs to resolve another provisioner by name (composing over more than one
/// candidate) is given this provider when its factory is constructed, so nothing resolves it from the
/// container at create time — which is what recursed without bound when it did (FDW-615).
/// </remarks>
public class ExternalIdentityProvisionerServiceProvider
    : DomainServiceProviderBase<
        IExternalIdentityProvisioner,
        IExternalIdentityProvisionerImplementationConfiguration,
        IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>,
        IExternalIdentityProvisionerConfigurationProvider>,
      IExternalIdentityProvisionerServiceProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalIdentityProvisionerServiceProvider"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public ExternalIdentityProvisionerServiceProvider(
        ILogger<DomainServiceProviderBase<
            IExternalIdentityProvisioner,
            IExternalIdentityProvisionerImplementationConfiguration,
            IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>,
            IExternalIdentityProvisionerConfigurationProvider>> logger)
        : base(logger ?? NullLogger<DomainServiceProviderBase<
            IExternalIdentityProvisioner,
            IExternalIdentityProvisionerImplementationConfiguration,
            IExternalIdentityProvisionerFactory<IExternalIdentityProvisioner, IExternalIdentityProvisionerImplementationConfiguration>,
            IExternalIdentityProvisionerConfigurationProvider>>.Instance)
    {
    }
}
