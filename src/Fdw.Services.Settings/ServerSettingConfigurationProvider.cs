using Fdw.Services.Settings.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings;

/// <summary>Supplies the ServerSetting configuration.</summary>
public sealed class ServerSettingConfigurationProvider
    : DomainConfigurationProviderBase<IServerSettingImplementationConfiguration>,
      IServerSettingConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="ServerSettingConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public ServerSettingConfigurationProvider(
        ILogger<ServerSettingConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "settings", "ServerSetting")
    {
    }
}
