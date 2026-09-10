using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Quality;

/// <summary>Supplies the configured Environment members.</summary>
public sealed class EnvironmentConfigurationProvider
    : DomainConfigurationProviderBase<IEnvironmentImplementationConfiguration>,
      IEnvironmentConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="EnvironmentConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public EnvironmentConfigurationProvider(
        ILogger<EnvironmentConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "quality", "Environment")
    {
    }
}
