using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization;

/// <summary>Supplies the Permission implementation's own configuration.</summary>
public sealed class PermissionImplementationConfigurationProvider
    : ImplementationProviderBase<PermissionImplementationConfiguration, IPermissionImplementationConfiguration>,
      IPermissionImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="PermissionImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public PermissionImplementationConfigurationProvider(
        ILogger<PermissionImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "authz", "PermissionImplementation")
    {
    }
}
