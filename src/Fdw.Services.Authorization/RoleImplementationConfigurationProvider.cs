using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization;

/// <summary>Supplies the Role implementation's own configuration.</summary>
public sealed class RoleImplementationConfigurationProvider
    : ImplementationProviderBase<RoleImplementationConfiguration, IRoleImplementationConfiguration>,
      IRoleImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="RoleImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public RoleImplementationConfigurationProvider(
        ILogger<RoleImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "authz", "RoleImplementation")
    {
    }
}
