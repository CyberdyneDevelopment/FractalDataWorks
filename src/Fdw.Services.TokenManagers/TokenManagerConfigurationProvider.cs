using System;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
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
public class TokenManagerConfigurationProvider : DefaultConfigurationProvider<TokenManagerConfiguration, TokenManagerConfigurationCommand>
{
    /// <summary>
    /// Registers the TokenManagerConfigurationProvider and interface forwardings with DI, targeting
    /// this domain's own default location. To override, call <c>SetConfiguration</c> on the resolved
    /// singleton.
    /// </summary>
    public static void RegisterDomainServices(IServiceCollection services)
    {
        services.TryAddSingleton<TokenManagerConfigurationProvider>(sp =>
            new TokenManagerConfigurationProvider(
                sp.GetService<ILogger<TokenManagerConfigurationProvider>>()!,
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                invalidator: new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));

        services.TryAddSingleton<DefaultConfigurationProvider<TokenManagerConfiguration, TokenManagerConfigurationCommand>>(
            sp => sp.GetRequiredService<TokenManagerConfigurationProvider>());

        services.TryAddSingleton<IServiceConfigurationProvider<TokenManagerConfiguration>>(sp =>
            sp.GetRequiredService<TokenManagerConfigurationProvider>());
    }

    /// <summary>Initializes a new instance of the <see cref="TokenManagerConfigurationProvider"/> class.</summary>
    public TokenManagerConfigurationProvider(
        ILogger<TokenManagerConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "auth",
        Lazy<ICacheInvalidator?>? invalidator = null)
        : base(logger ?? NullLogger<TokenManagerConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName,
               invalidator)
    {
    }
}
