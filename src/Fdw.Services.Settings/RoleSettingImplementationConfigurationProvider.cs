using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Settings.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Settings;

/// <summary>Supplies the RoleSetting implementation's own configuration.</summary>
public sealed class RoleSettingImplementationConfigurationProvider
    : ImplementationProviderBase<RoleSettingImplementationConfiguration, IRoleSettingImplementationConfiguration>,
      IRoleSettingImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="RoleSettingImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public RoleSettingImplementationConfigurationProvider(
        ILogger<RoleSettingImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "settings", "RoleSettingImplementation")
    {
    }
}
