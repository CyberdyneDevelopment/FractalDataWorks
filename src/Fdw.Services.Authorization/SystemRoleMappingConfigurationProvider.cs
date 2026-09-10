using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization;

/// <summary>Supplies the SystemRoleMapping implementation's own configuration.</summary>
public sealed class SystemRoleMappingConfigurationProvider
    : ImplementationProviderBase<SystemRoleMappingConfiguration, IRoleMappingImplementationConfiguration>,
      ISystemRoleMappingConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="SystemRoleMappingConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public SystemRoleMappingConfigurationProvider(
        ILogger<SystemRoleMappingConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "authz", "SystemRoleMapping")
    {
    }
}
