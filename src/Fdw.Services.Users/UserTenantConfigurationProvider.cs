using Fdw.Commands.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Users.Commands;
using Fdw.Services.Users.Configuration;
using Fdw.Services.Users.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users;

/// <summary>Supplies the UserTenants configuration.</summary>
public sealed class UserTenantConfigurationProvider
    : ImplementationConfigurationProviderBase<IUserTenantImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="UserTenantConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public UserTenantConfigurationProvider(
        ILogger<UserTenantConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "tenant", "UserTenants")
    {
    }
}
