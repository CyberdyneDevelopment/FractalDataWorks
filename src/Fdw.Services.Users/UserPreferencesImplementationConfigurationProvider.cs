using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Users.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users;

/// <summary>Supplies the UserPreferences implementation's own configuration.</summary>
public sealed class UserPreferencesImplementationConfigurationProvider
    : ImplementationProviderBase<UserPreferencesImplementationConfiguration, IUserPreferencesImplementationConfiguration>,
      IUserPreferencesImplementationConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="UserPreferencesImplementationConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public UserPreferencesImplementationConfigurationProvider(
        ILogger<UserPreferencesImplementationConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "usr", "UserPreferencesImplementation")
    {
    }
}
