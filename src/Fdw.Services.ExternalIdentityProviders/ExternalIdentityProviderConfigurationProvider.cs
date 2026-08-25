using System;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.ExternalIdentityProviders;

/// <summary>
/// Configuration provider for ExternalIdentityProviderConfiguration rows in auth.ExternalIdentityProvider.
/// Reads through IConfigurationGateway — no IConfiguration binding section.
/// </summary>
// Why: ExternalIdentityProviderConfiguration is loaded from ConfigurationDb at runtime via
// Lazy<IConfigurationGateway>, not through BindConfiguration("ExternalIdentityProviders:..."). Mirrors
// TokenManagerConfigurationProvider exactly.
public class ExternalIdentityProviderConfigurationProvider : DefaultConfigurationProvider<ExternalIdentityProviderConfiguration, ExternalIdentityProviderConfigurationCommand>
{
    /// <summary>
    /// Registers the ExternalIdentityProviderConfigurationProvider and interface forwardings with DI,
    /// targeting this domain's own default location. To override, call <c>SetConfiguration</c> on the
    /// resolved singleton.
    /// </summary>
    public static void RegisterDomainServices(IServiceCollection services)
    {
        services.TryAddSingleton<ExternalIdentityProviderConfigurationProvider>(sp =>
            new ExternalIdentityProviderConfigurationProvider(
                sp.GetService<ILogger<ExternalIdentityProviderConfigurationProvider>>()!,
                sp.GetRequiredService<Lazy<IConfigurationGateway>>()));

        services.TryAddSingleton<DefaultConfigurationProvider<ExternalIdentityProviderConfiguration, ExternalIdentityProviderConfigurationCommand>>(
            sp => sp.GetRequiredService<ExternalIdentityProviderConfigurationProvider>());

        services.TryAddSingleton<IServiceConfigurationProvider<ExternalIdentityProviderConfiguration>>(sp =>
            sp.GetRequiredService<ExternalIdentityProviderConfigurationProvider>());
    }

    /// <summary>Initializes a new instance of the <see cref="ExternalIdentityProviderConfigurationProvider"/> class.</summary>
    public ExternalIdentityProviderConfigurationProvider(
        ILogger<ExternalIdentityProviderConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "auth")
        : base(logger ?? NullLogger<ExternalIdentityProviderConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName)
    {
    }
}
