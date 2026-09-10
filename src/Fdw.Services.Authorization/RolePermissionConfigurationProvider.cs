using Fdw.Services.Authorization.Commands;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization;

/// <summary>Supplies the RolePermission configuration.</summary>
public sealed class RolePermissionConfigurationProvider
    : DomainConfigurationProviderBase<IRolePermissionImplementationConfiguration>,
      IRolePermissionConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="RolePermissionConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public RolePermissionConfigurationProvider(
        ILogger<RolePermissionConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "authz", "RolePermission")
    {
    }
}
