using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Users.Commands;
using Fdw.Services.Users.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Users;

/// <summary>
/// Reads the users domain's own configuration row.
/// </summary>
/// <remarks>
/// Sits beside UserConfigurationProvider on the same store and path, because the settings that
/// govern the users domain are the users domain's data -- not a section of the host's
/// configuration file that every consumer has to be handed through IOptions.
/// </remarks>
public class UsersServiceConfigurationProvider
    : ImplementationConfigurationProviderBase<UsersServiceConfiguration, UsersServiceConfigurationCommand>
{
    /// <summary>Initializes a new instance of the <see cref="UsersServiceConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger for this provider.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the store these rows live on.</param>
    /// <param name="dataStoreName">The store the domain's rows live in.</param>
    /// <param name="pathName">The path the rows live under.</param>
    public UsersServiceConfigurationProvider(
        ILogger<UsersServiceConfigurationProvider>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "usr")
        : base(logger ?? NullLogger<UsersServiceConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName,
               pathName)
    {
    }
}
