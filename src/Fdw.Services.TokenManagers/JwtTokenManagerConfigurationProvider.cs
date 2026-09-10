using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.TokenManagers.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.TokenManagers;

/// <summary>Supplies the JwtTokenManager configuration.</summary>
public sealed class JwtTokenManagerConfigurationProvider
    : ImplementationProviderBase<JwtTokenManagerConfiguration>
{
    /// <summary>Initializes a new instance of the <see cref="JwtTokenManagerConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public JwtTokenManagerConfigurationProvider(
        ILogger<JwtTokenManagerConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "auth", "JwtTokenManager")
    {
    }
}
