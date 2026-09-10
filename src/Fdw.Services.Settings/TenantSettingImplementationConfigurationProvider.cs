using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Settings.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings;

/// <summary>Supplies the TenantSetting implementation's own configuration.</summary>
public sealed class TenantSettingImplementationConfigurationProvider
    : ImplementationProviderBase<TenantSettingImplementationConfiguration, ITenantSettingImplementationConfiguration>,
      ITenantSettingImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="TenantSettingImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public TenantSettingImplementationConfigurationProvider(
        ILogger<TenantSettingImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "settings", "TenantSettingImplementation")
    {
    }
}
