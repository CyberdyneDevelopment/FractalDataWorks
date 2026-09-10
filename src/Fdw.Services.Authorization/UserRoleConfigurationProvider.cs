using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authorization.Commands;
using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Authorization.Logging;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization;

/// <summary>Supplies the UserRole configuration.</summary>
public sealed class UserRoleConfigurationProvider
    : ImplementationConfigurationProviderBase<IUserRoleImplementationConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="UserRoleConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public UserRoleConfigurationProvider(
        ILogger<UserRoleConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "authz", "UserRole")
    {
    }
}
