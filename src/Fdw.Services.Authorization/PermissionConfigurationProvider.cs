using Fdw.Services.Authorization.Commands;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization;

/// <summary>Supplies the Permission domain configuration.</summary>
public sealed class PermissionConfigurationProvider
    : DomainConfigurationProviderBase<IPermissionImplementationConfiguration>,
      IPermissionConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="PermissionConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public PermissionConfigurationProvider(
        ILogger<PermissionConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "authz", "Permission")
    {
    }
}
