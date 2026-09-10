using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Users.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users;

/// <summary>Supplies the UsersService implementation's own configuration.</summary>
public sealed class UsersServiceImplementationConfigurationProvider
    : ImplementationProviderBase<UsersServiceImplementationConfiguration, IUsersServiceImplementationConfiguration>,
      IUsersServiceImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="UsersServiceImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public UsersServiceImplementationConfigurationProvider(
        ILogger<UsersServiceImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "usr", "UsersServiceImplementation")
    {
    }
}
