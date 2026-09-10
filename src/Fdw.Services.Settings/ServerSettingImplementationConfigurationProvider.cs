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
    public ServerSettingImplementationConfigurationProvider(
        ILogger<ServerSettingImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "settings", "ServerSettingImplementation")
    {
    }
}
