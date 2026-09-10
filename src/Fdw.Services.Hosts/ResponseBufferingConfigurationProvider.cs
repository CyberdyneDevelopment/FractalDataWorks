using Fdw.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Hosts.Abstractions;
using Fdw.Services.Hosts.Commands;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Hosts;

/// <summary>Supplies the ResponseBuffering configuration.</summary>
public sealed class ResponseBufferingConfigurationProvider
    : DomainConfigurationProviderBase<IResponseBufferingImplementationConfiguration>,
      IResponseBufferingConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="ResponseBufferingConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public ResponseBufferingConfigurationProvider(
        ILogger<ResponseBufferingConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "hst", "ResponseBuffering")
    {
    }
}
