using System;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Identity.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Identity;

/// <summary>
/// Configuration provider for IdentityServiceConfiguration rows in sec.Identity.
/// Reads through IConfigurationGateway — no IConfiguration binding section.
/// </summary>
// Why: IdentityServiceConfiguration is loaded from ConfigurationDb at runtime via
// Lazy<IConfigurationGateway>, not through BindConfiguration("Identities:..."). Mirrors
// TokenManagerConfigurationProvider / SecretManagerConfigurationProvider exactly.
public class IdentityServiceConfigurationProvider
    : ServiceConfigurationProviderBase<
          IdentityServiceConfiguration,
          IIdentityServiceImplementationConfiguration,
          IdentityServiceConfigurationCommand>,
      IIdentityServiceConfigurationProvider
{
    /// <summary>
    /// Registers the IdentityServiceConfigurationProvider and interface forwardings with DI,
    /// targeting this domain's own default location. To override, call <c>SetConfiguration</c> on
    /// the resolved singleton.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    public static void RegisterDomainServices(IServiceCollection services)
    {
        services.TryAddSingleton<IdentityServiceConfigurationProvider>(sp =>
            new IdentityServiceConfigurationProvider(
                sp.GetService<ILogger<IdentityServiceConfigurationProvider>>()!,
                sp.GetRequiredService<Lazy<IConfigurationGateway>>()));

        services.TryAddSingleton<ImplementationConfigurationProviderBase<IdentityServiceConfiguration, IdentityServiceConfigurationCommand>>(
            sp => sp.GetRequiredService<IdentityServiceConfigurationProvider>());

        services.TryAddSingleton<IServiceConfigurationProvider<IdentityServiceConfiguration>>(sp =>
            sp.GetRequiredService<IdentityServiceConfigurationProvider>());
    }

    /// <summary>Initializes a new instance of the <see cref="IdentityServiceConfigurationProvider"/> class.</summary>
    /// <param name="logger">The logger for this provider.</param>
    /// <param name="lazyGateway">Deferred configuration gateway, resolved on the first configuration read.</param>
    /// <param name="dataStoreName">The data store holding the configuration.</param>
    /// <param name="pathName">The schema holding the configuration.</param>
    public IdentityServiceConfigurationProvider(
        ILogger<IdentityServiceConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "sec")
        : base(logger ?? NullLogger<IdentityServiceConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName)
    {
    }

    /// <inheritdoc />
    protected override IdentityServiceConfiguration Compose<T>(
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
