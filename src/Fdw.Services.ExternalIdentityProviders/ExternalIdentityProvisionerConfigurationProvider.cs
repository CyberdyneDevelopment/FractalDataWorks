using Fdw.Configuration;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Commands;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.ExternalIdentityProviders;

/// <summary>Supplies the ExternalIdentityProvisioner configuration.</summary>
public sealed class ExternalIdentityProvisionerConfigurationProvider
    : DomainConfigurationProviderBase<IExternalIdentityProvisionerImplementationConfiguration>,
      IExternalIdentityProvisionerConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="ExternalIdentityProvisionerConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public ExternalIdentityProvisionerConfigurationProvider(
        ILogger<ExternalIdentityProvisionerConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "sec", "ExternalIdentityProvisioner")
    {
    }
}
