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
public class PermissionConfigurationProvider
    : ImplementationConfigurationProviderBase<PermissionConfiguration, PermissionConfigurationCommand>,
      IPermissionConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="PermissionConfigurationProvider"/> class.</summary>
    public PermissionConfigurationProvider(
        ILogger<PermissionConfigurationProvider>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "authz")
        : base(logger ?? NullLogger<PermissionConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
    }
}
