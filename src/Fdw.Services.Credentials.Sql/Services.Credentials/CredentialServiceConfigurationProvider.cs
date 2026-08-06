using System;
using System.Collections.Generic;
using Fdw.Configuration;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Credentials;

/// <summary>
/// Domain-specific configuration provider for credential services.
/// The polymorphic typed-body read (dispatch on <c>ServiceOptionType</c> to load the typed body row and
/// attach it to <see cref="CredentialServiceConfiguration.Configuration"/>) is composed uniformly by
/// <see cref="DefaultConfigurationProvider{TConfig,TCommand}"/>; typed providers are registered via the
/// inherited <c>RegisterTypedProvider</c>.
/// </summary>
public class CredentialServiceConfigurationProvider : DefaultConfigurationProvider<CredentialServiceConfiguration, CredentialServiceConfigurationCommand>
{
    /// <summary>
    /// Registers the CredentialServiceConfigurationProvider with DI, targeting this domain's own
    /// default location. To override, call <c>SetConfiguration</c> on the resolved singleton.
    /// </summary>
    public static void RegisterDomainConfiguration(IServiceCollection services)
    {
        services.TryAddSingleton<CredentialServiceConfigurationProvider>(sp =>
            new CredentialServiceConfigurationProvider(
                sp.GetService<ILogger<CredentialServiceConfigurationProvider>>()!,
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                invalidator: new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));
        // Why: Consumers inject DefaultConfigurationProvider<TConfig, TCommand> (the base) —
        // forward to the concrete subclass so injection by base type succeeds.
        services.TryAddSingleton<DefaultConfigurationProvider<CredentialServiceConfiguration, CredentialServiceConfigurationCommand>>(
            sp => sp.GetRequiredService<CredentialServiceConfigurationProvider>());
        // Why: Generated provider wiring links IServiceConfigurationProvider<T> as the parent on the
        // domain provider (CredentialServiceProvider); this forward lets that lookup succeed.
        services.TryAddSingleton<IServiceConfigurationProvider<CredentialServiceConfiguration>>(
            sp => sp.GetRequiredService<CredentialServiceConfigurationProvider>());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialServiceConfigurationProvider"/> class.
    /// </summary>
    public CredentialServiceConfigurationProvider(
        ILogger<CredentialServiceConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "sec",
        Lazy<ICacheInvalidator?>? invalidator = null)
        : base(logger ?? NullLogger<CredentialServiceConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName,
               invalidator)
    {
    }
}
