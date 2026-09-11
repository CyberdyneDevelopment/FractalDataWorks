using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Hosts.Abstractions;
using Fdw.Services.Hosts.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Hosts;

/// <summary>Supplies the Host implementation's own configuration.</summary>
public sealed class HostImplementationConfigurationProvider
    : ImplementationProviderBase<HostImplementationConfiguration, IHostImplementationConfiguration>,
      IHostImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="HostImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The store this provider's rows live in -- passed by its registration.</param>
    public HostImplementationConfigurationProvider(
        ILogger<HostImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "hst", "HostImplementation")
    {
    }
}
