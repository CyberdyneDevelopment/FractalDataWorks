using Fdw.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Hosts.Abstractions;
using Fdw.Services.Hosts.Commands;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Hosts;

/// <summary>Supplies the ForwardedHeaders domain configuration.</summary>
public sealed class ForwardedHeadersConfigurationProvider
    : DomainConfigurationProviderBase<IForwardedHeadersImplementationConfiguration>,
      IForwardedHeadersConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="ForwardedHeadersConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public ForwardedHeadersConfigurationProvider(
        ILogger<ForwardedHeadersConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "hst", "ForwardedHeaders")
    {
    }
}
