using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Hosts.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Hosts;

/// <summary>Supplies the EmptyBody configuration.</summary>
public sealed class EmptyBodyConfigurationProvider
    : DomainConfigurationProviderBase<IEmptyBodyImplementationConfiguration>,
      IEmptyBodyConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="EmptyBodyConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public EmptyBodyConfigurationProvider(
        ILogger<EmptyBodyConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "hst", "EmptyBody")
    {
    }
}
