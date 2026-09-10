using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Commands;
using Fdw.Services.Data.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>Supplies the MainDataGateway configuration.</summary>
public sealed class MainDataGatewayConfigurationProvider
    : ImplementationProviderBase<MainDataGatewayConfiguration, IDataGatewayImplementationConfiguration>,
      IMainDataGatewayConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="MainDataGatewayConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public MainDataGatewayConfigurationProvider(
        ILogger<MainDataGatewayConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "dg", "MainDataGateway")
    {
    }
}
