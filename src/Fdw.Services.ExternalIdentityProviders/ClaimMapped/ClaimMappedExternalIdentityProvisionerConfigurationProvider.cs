using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.ExternalIdentityProviders.ClaimMapped;

/// <summary>Supplies the ClaimMappedExternalIdentityProvisioner configuration.</summary>
public sealed class ClaimMappedExternalIdentityProvisionerConfigurationProvider
    : ImplementationProviderBase<ClaimMappedExternalIdentityProvisionerConfiguration, IExternalIdentityProvisionerImplementationConfiguration>,
      IClaimMappedExternalIdentityProvisionerConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="ClaimMappedExternalIdentityProvisionerConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public ClaimMappedExternalIdentityProvisionerConfigurationProvider(
        ILogger<ClaimMappedExternalIdentityProvisionerConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "sec", "ClaimMappedExternalIdentityProvisioner")
    {
    }
}
