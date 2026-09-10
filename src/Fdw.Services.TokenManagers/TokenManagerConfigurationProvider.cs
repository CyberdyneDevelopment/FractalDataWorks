using Fdw.Configuration;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.TokenManagers.Abstractions;
using Fdw.Services.TokenManagers.Commands;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.TokenManagers;

/// <summary>Supplies the TokenManager configuration.</summary>
public sealed class TokenManagerConfigurationProvider
    : DomainConfigurationProviderBase<ITokenManagerImplementationConfiguration>,
      ITokenManagerConfigurationProvider
{
    /// <summary>Initializes a new instance of the <see cref="TokenManagerConfigurationProvider"/> class.</summary>
    /// <param name="logger">Logger for this provider instance.</param>
    /// <param name="gatewayProvider">Supplies the gateway onto the configuration store.</param>
    public TokenManagerConfigurationProvider(
        ILogger<TokenManagerConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider)
        : base(logger, gatewayProvider, "PlatformConfiguration", "auth", "TokenManager")
    {
    }
}
