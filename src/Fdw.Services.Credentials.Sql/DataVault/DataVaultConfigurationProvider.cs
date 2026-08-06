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

namespace Fdw.Services.DataVault;

/// <summary>
/// Domain-specific configuration provider for data vaults.
/// The polymorphic typed-body read (dispatch on <c>ServiceOptionType</c> to load the typed body row and
/// attach it to <see cref="DataVaultConfiguration.Configuration"/>) is composed uniformly by
/// <see cref="DefaultConfigurationProvider{TConfig,TCommand}"/>; typed providers are registered via the
/// inherited <c>RegisterTypedProvider</c>.
/// </summary>
public class DataVaultConfigurationProvider : DefaultConfigurationProvider<DataVaultConfiguration, DataVaultConfigurationCommand>
{
    /// <summary>
    /// Registers the DataVaultConfigurationProvider with DI, targeting this domain's own default
    /// location. To override, call <c>SetConfiguration</c> on the resolved singleton.
    /// </summary>
    public static void RegisterDomainConfiguration(IServiceCollection services)
    {
        services.TryAddSingleton<DataVaultConfigurationProvider>(sp =>
            new DataVaultConfigurationProvider(
                sp.GetService<ILogger<DataVaultConfigurationProvider>>()!,
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                invalidator: new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));
        // Why: Consumers inject DefaultConfigurationProvider<TConfig, TCommand> (the base) —
        // forward to the concrete subclass so injection by base type succeeds.
        services.TryAddSingleton<DefaultConfigurationProvider<DataVaultConfiguration, DataVaultConfigurationCommand>>(
            sp => sp.GetRequiredService<DataVaultConfigurationProvider>());
        // Why: Generated Initialize() links IServiceConfigurationProvider<T> as the parent on the
        // domain provider (DataVaultProvider); this forward lets that lookup succeed.
        services.TryAddSingleton<IServiceConfigurationProvider<DataVaultConfiguration>>(
            sp => sp.GetRequiredService<DataVaultConfigurationProvider>());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataVaultConfigurationProvider"/> class.
    /// </summary>
    public DataVaultConfigurationProvider(
        ILogger<DataVaultConfigurationProvider> logger,
        Lazy<IConfigurationGateway> lazyGateway,
        string dataStoreName = "ConfigurationDb",
        string pathName = "sec",
        Lazy<ICacheInvalidator?>? invalidator = null)
        : base(logger ?? NullLogger<DataVaultConfigurationProvider>.Instance,
               lazyGateway,
               dataStoreName, pathName,
               invalidator)
    {
    }
}
