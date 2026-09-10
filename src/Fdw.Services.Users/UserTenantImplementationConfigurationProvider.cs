using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Users.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users;

/// <summary>Supplies the UserTenants implementation's own configuration.</summary>
public sealed class UserTenantImplementationConfigurationProvider
    : ImplementationProviderBase<UserTenantImplementationConfiguration, IUserTenantImplementationConfiguration>,
      IUserTenantImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="UserTenantImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public UserTenantImplementationConfigurationProvider(
        ILogger<UserTenantImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "tenant", "UserTenantsImplementation")
    {
    }
}
