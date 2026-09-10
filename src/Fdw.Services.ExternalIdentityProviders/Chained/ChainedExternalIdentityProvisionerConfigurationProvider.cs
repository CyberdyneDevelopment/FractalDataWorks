using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.ExternalIdentityProviders.Chained;

/// <summary>Supplies the ChainedExternalIdentityProvisioner configuration.</summary>
public sealed class ChainedExternalIdentityProvisionerConfigurationProvider
    : ImplementationProviderBase<ChainedExternalIdentityProvisionerConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="ChainedExternalIdentityProvisionerConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public ChainedExternalIdentityProvisionerConfigurationProvider(
        ILogger<ChainedExternalIdentityProvisionerConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "sec", "ChainedExternalIdentityProvisioner")
    {
    }
}
