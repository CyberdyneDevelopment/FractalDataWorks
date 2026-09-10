using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Settings.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings;

/// <summary>Supplies the ServerSetting implementation's own configuration.</summary>
public sealed class ServerSettingImplementationConfigurationProvider
    : ImplementationProviderBase<ServerSettingImplementationConfiguration, IServerSettingImplementationConfiguration>,
      IServerSettingImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="ServerSettingImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public ServerSettingImplementationConfigurationProvider(
        ILogger<ServerSettingImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "settings", "ServerSettingImplementation")
    {
    }
}
