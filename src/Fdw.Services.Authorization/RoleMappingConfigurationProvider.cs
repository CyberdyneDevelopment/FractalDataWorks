using Fdw.Configuration;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Commands;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization;

/// <summary>Supplies the RoleMapping domain configuration.</summary>
public sealed class RoleMappingConfigurationProvider
    : DomainConfigurationProviderBase<IRoleMappingImplementationConfiguration>,
      IRoleMappingConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="RoleMappingConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public RoleMappingConfigurationProvider(
        ILogger<RoleMappingConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "authz", "RoleMapping")
    {
    }
}
