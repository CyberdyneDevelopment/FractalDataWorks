using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings;

/// <summary>Supplies the configured TenantSetting members.</summary>
public sealed class TenantSettingConfigurationProvider
    : DomainConfigurationProviderBase<ITenantSettingImplementationConfiguration>,
      ITenantSettingConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="TenantSettingConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public TenantSettingConfigurationProvider(
        ILogger<TenantSettingConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "settings", "TenantSetting")
    {
    }
}
