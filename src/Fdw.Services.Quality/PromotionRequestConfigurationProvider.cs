using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Quality;

/// <summary>Supplies the PromotionRequest configuration.</summary>
public sealed class PromotionRequestConfigurationProvider
    : DomainConfigurationProviderBase<IPromotionRequestImplementationConfiguration>,
      IPromotionRequestConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="PromotionRequestConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public PromotionRequestConfigurationProvider(
        ILogger<PromotionRequestConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "quality", "PromotionRequest")
    {
    }
}
