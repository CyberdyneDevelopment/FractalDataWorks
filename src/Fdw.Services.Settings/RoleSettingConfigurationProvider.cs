using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings;

/// <summary>Supplies the RoleSetting configuration.</summary>
public sealed class RoleSettingConfigurationProvider
    : DomainConfigurationProviderBase<IRoleSettingImplementationConfiguration>,
      IRoleSettingConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="RoleSettingConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public RoleSettingConfigurationProvider(
        ILogger<RoleSettingConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "settings", "RoleSetting")
    {
    }
}
