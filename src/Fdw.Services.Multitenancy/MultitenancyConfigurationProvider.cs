using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Multitenancy;

/// <summary>Supplies the multitenancy implementation this deployment runs.</summary>
public sealed class MultitenancyConfigurationProvider
    : DomainConfigurationProviderBase<IMultitenancyImplementationConfiguration>,
      IMultitenancyConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="MultitenancyConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public MultitenancyConfigurationProvider(
        ILogger<MultitenancyConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "settings", "Multitenancy")
    {
    }
}
