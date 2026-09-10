using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.ExternalIdentityProviders.Chained;

/// <summary>Supplies the ChainedExternalIdentityProvisioner configuration.</summary>
public sealed class ChainedExternalIdentityProvisionerConfigurationProvider
    : ImplementationProviderBase<ChainedExternalIdentityProvisionerConfiguration, IExternalIdentityProvisionerImplementationConfiguration>,
      IChainedExternalIdentityProvisionerConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="ChainedExternalIdentityProvisionerConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public ChainedExternalIdentityProvisionerConfigurationProvider(
        ILogger<ChainedExternalIdentityProvisionerConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "sec", "ChainedExternalIdentityProvisioner")
    {
    }
}
