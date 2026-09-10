using Fdw.Configuration;
using System;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.TokenManagers.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.TokenManagers.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.TokenManagers;

/// <summary>
/// Configuration provider for TokenManagerConfiguration rows in auth.TokenManager.
/// Reads through IConfigurationGateway — no IConfiguration binding section.
/// </summary>
public class TokenManagerConfigurationProvider
    : ImplementationConfigurationProviderBase<DomainConfiguration, ITokenManagerImplementationConfiguration>,
      ITokenManagerConfigurationProvider
{


    /// <summary>Initializes a new instance of the <see cref="TokenManagerConfigurationProvider"/> class.</summary>
    public TokenManagerConfigurationProvider(
        ILogger<TokenManagerConfigurationProvider> logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName,
        string pathName = "auth")
        : base(logger ?? NullLogger<TokenManagerConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName,
               "TokenManager")
    {
    }
}
