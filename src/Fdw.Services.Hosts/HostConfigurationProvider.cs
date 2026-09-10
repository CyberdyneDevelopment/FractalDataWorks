using Fdw.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Hosts.Abstractions;
using Fdw.Services.Hosts.Commands;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Hosts;

/// <summary>Supplies the Host configuration.</summary>
public sealed class HostConfigurationProvider
    : DomainConfigurationProviderBase<IHostImplementationConfiguration>,
      IHostConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="HostConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public HostConfigurationProvider(
        ILogger<HostConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "hst", "Host")
    {
    }
}
