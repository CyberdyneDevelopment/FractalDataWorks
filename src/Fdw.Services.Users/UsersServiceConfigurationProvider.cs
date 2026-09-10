using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Users.Commands;
using Fdw.Services.Users.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users;

/// <summary>Supplies the UsersService configuration.</summary>
public sealed class UsersServiceConfigurationProvider
    : DomainConfigurationProviderBase<IUsersServiceImplementationConfiguration>,
      IUsersServiceConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="UsersServiceConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public UsersServiceConfigurationProvider(
        ILogger<UsersServiceConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "usr", "UsersService")
    {
    }
}
