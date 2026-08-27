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
// Why: TokenManagerConfiguration is loaded from ConfigurationDb at runtime via
// Lazy<IConfigurationGateway>, not through BindConfiguration("TokenManagers:..."). Mirrors
// SchedulerConfigurationProvider/AuthenticationServiceConfigurationProvider exactly.
public class TokenManagerConfigurationProvider
    : ServiceConfigurationProviderBase<
          TokenManagerConfiguration,
          ITokenManagerImplementationConfiguration,
          TokenManagerConfigurationCommand>,
      ITokenManagerConfigurationProvider
{

    /// <summary>Initializes a new instance of the <see cref="TokenManagerConfigurationProvider"/> class.</summary>
    public TokenManagerConfigurationProvider(
        ILogger<TokenManagerConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "auth")
        : base(logger ?? NullLogger<TokenManagerConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName)
    {
    }

    /// <inheritdoc />
    protected override TokenManagerConfiguration Compose<T>(
        string serviceOptionType,
        string name,
        T implementationConfiguration)
        => new()
        {
            Name = name,
            ServiceOptionType = serviceOptionType,
            Configuration = implementationConfiguration,
        };
}
