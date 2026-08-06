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
/// Configuration provider for ExternalIdentityProvisionerConfiguration rows in
/// sec.ExternalIdentityProvisioner. Reads through IConfigurationGateway — no IConfiguration binding
/// section.
/// </summary>
// Why: ExternalIdentityProvisionerConfiguration is loaded from ConfigurationDb at runtime via
// Lazy<IConfigurationGateway>, not through BindConfiguration("ExternalIdentityProvisioners:..."). Mirrors
// ExternalIdentityProviderConfigurationProvider exactly, targeting sec instead of auth.
public class ExternalIdentityProvisionerConfigurationProvider : DefaultConfigurationProvider<ExternalIdentityProvisionerConfiguration, ExternalIdentityProvisionerConfigurationCommand>
{
    /// <summary>
    /// Registers the ExternalIdentityProvisionerConfigurationProvider and interface forwardings with
    /// DI, targeting this domain's own default location. To override, call <c>SetConfiguration</c> on
    /// the resolved singleton.
    /// </summary>
    public static void RegisterDomainServices(IServiceCollection services)
    {
        services.TryAddSingleton<ExternalIdentityProvisionerConfigurationProvider>(sp =>
            new ExternalIdentityProvisionerConfigurationProvider(
                sp.GetService<ILogger<ExternalIdentityProvisionerConfigurationProvider>>()!,
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                invalidator: new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));

        services.TryAddSingleton<DefaultConfigurationProvider<ExternalIdentityProvisionerConfiguration, ExternalIdentityProvisionerConfigurationCommand>>(
            sp => sp.GetRequiredService<ExternalIdentityProvisionerConfigurationProvider>());

        services.TryAddSingleton<IServiceConfigurationProvider<ExternalIdentityProvisionerConfiguration>>(sp =>
            sp.GetRequiredService<ExternalIdentityProvisionerConfigurationProvider>());
    }

    /// <summary>Initializes a new instance of the <see cref="ExternalIdentityProvisionerConfigurationProvider"/> class.</summary>
    public ExternalIdentityProvisionerConfigurationProvider(
        ILogger<ExternalIdentityProvisionerConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "sec",
        Lazy<ICacheInvalidator?>? invalidator = null)
        : base(logger ?? NullLogger<ExternalIdentityProvisionerConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName,
               invalidator)
    {
    }
}
