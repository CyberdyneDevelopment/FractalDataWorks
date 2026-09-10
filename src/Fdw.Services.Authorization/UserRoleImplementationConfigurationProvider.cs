using Fdw.Services.Authorization.Configuration;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authorization;

/// <summary>Supplies the UserRole implementation's own configuration.</summary>
public sealed class UserRoleImplementationConfigurationProvider
    : ImplementationProviderBase<UserRoleImplementationConfiguration, IUserRoleImplementationConfiguration>,
      IUserRoleImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="UserRoleImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public UserRoleImplementationConfigurationProvider(
        ILogger<UserRoleImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "authz", "UserRoleImplementation")
    {
    }
}
