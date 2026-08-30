using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.TokenManagers;

/// <summary>
/// Reads <c>auth.JwtTokenManager</c> rows by their parent header's id.
/// </summary>
/// <remarks>
/// The header provider loads <c>auth.TokenManager</c>, then dispatches here on
/// <c>ServiceOptionType = "Jwt"</c> to fill the typed body. <c>Get(Guid)</c> takes the parent
/// <c>auth.TokenManager.Id</c>, not this row's own id.
/// </remarks>
public class JwtTokenManagerConfigurationProvider
    : ImplementationConfigurationProvider<
        ITokenManagerImplementationConfiguration,
        JwtTokenManagerConfiguration,
        JwtTokenManagerConfigurationCommand>
{
    /// <summary>Initializes a new instance of the <see cref="JwtTokenManagerConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="gatewayProvider">The configuration gateway provider.</param>
    /// <param name="dataStoreName">The connection these rows are read from.</param>
    /// <param name="pathName">The schema path.</param>
    public JwtTokenManagerConfigurationProvider(
        ILogger<JwtTokenManagerConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "auth")
        : base(logger ?? NullLogger<JwtTokenManagerConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName,
               pathName)
    {
    }
}
