using Fdw.Services.Authorization.Commands;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authorization;

/// <summary>
/// Reads the permissions this platform defines.
/// </summary>
public class RolePermissionConfigurationProvider
    : ImplementationConfigurationProviderBase<RolePermissionConfiguration, RolePermissionConfigurationCommand>,
      IRolePermissionConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="RolePermissionConfigurationProvider"/> class.</summary>
    public RolePermissionConfigurationProvider(
        ILogger<RolePermissionConfigurationProvider>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "authz")
        : base(logger ?? NullLogger<RolePermissionConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
    }
}
