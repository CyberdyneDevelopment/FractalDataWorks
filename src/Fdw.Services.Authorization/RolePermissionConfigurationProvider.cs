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
    : ImplementationConfigurationProviderBase<RolePermissionConfiguration, IRolePermissionImplementationConfiguration, RolePermissionConfigurationCommand>,
      IRolePermissionConfigurationProvider
{
    /// <summary>Gets or sets the domain this implementation belongs to.</summary>
    /// <remarks>Set by the provider from the domain row; never persisted.</remarks>
    public string Domain { get; set; } = string.Empty;

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
