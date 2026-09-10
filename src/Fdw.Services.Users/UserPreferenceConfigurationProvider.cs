using Fdw.Commands.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Users.Commands;
using Fdw.Services.Users.Logging;
using Fdw.Services.Users.Models;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users;

/// <summary>Supplies the UserPreferences configuration.</summary>
public sealed class UserPreferenceConfigurationProvider
    : DomainConfigurationProviderBase<IUserPreferencesImplementationConfiguration>,
      IUserPreferenceConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="UserPreferenceConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    /// <param name="dataStoreName">The connection this domain's configuration rows are read from and written to.</param>
    public UserPreferenceConfigurationProvider(
        ILogger<UserPreferenceConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName)
        : base(logger, gatewayProvider, dataStoreName, "usr", "UserPreferences")
    {
    }
}
