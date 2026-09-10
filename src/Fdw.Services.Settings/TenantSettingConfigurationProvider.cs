using Fdw.Services.Settings.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings;

/// <summary>Supplies the TenantSetting configuration.</summary>
public sealed class TenantSettingConfigurationProvider
    : DomainConfigurationProviderBase<ITenantSettingImplementationConfiguration>,
      ITenantSettingConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="TenantSettingConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public TenantSettingConfigurationProvider(
        ILogger<TenantSettingConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "settings", "TenantSetting")
    {
    }
}
