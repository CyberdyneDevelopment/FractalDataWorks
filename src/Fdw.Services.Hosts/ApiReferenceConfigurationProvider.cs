using Fdw.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Hosts.Abstractions;
using Fdw.Services.Hosts.Commands;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Hosts;

/// <summary>Supplies the ApiReference domain configuration.</summary>
public sealed class ApiReferenceConfigurationProvider
    : DomainConfigurationProviderBase<IApiReferenceImplementationConfiguration>,
      IApiReferenceConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="ApiReferenceConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public ApiReferenceConfigurationProvider(
        ILogger<ApiReferenceConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "hst", "ApiReference")
    {
    }
}
