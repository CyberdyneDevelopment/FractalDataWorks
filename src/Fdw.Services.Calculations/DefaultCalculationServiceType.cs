using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Calculations.Abstractions.CalculationSources;
using Fdw.Services.Calculations.Abstractions.Caching;
using Fdw.Services.Calculations.Caching;
using Fdw.Services.Calculations.Commands;
using Fdw.Services.Calculations.Configuration;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Calculations;

/// <summary>
/// Default calculation service type that registers calculation entity services
/// (ICalculationEntityService, ICalculationEntityProvider, ICalculationInputResolver,
/// ICalculationCacheService) and the gateway-backed CalculationConfigurationProvider.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(CalculationServiceTypes), "Default")]
public sealed class DefaultCalculationServiceType : CalculationServiceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultCalculationServiceType"/> class.
    /// </summary>
    public DefaultCalculationServiceType()
        : base(
            "Default",
            "Calculation:Default",
            "Default Calculation Services",
            "Default calculation entity, provider, input-resolver, and cache services")
    {
        // Why Initialize and not Register: this wiring needs a LIVE container (it resolves the
        // domain provider and its typed-body providers), and Register runs while the container
        // is still being built. Initialize runs after Build() with a real IServiceProvider.
        Initialization((services, loggerFactory) =>
        {
            var header = services.GetRequiredService<CalculationConfigurationProvider>();

            header.RegisterTypedProvider(
                "Formula",
                services.GetRequiredService<DefaultConfigurationProvider<FormulaCalculationConfiguration, FormulaCalculationConfigurationCommand>>());

            header.RegisterTypedProvider(
                "Windowed",
                services.GetRequiredService<DefaultConfigurationProvider<WindowedCalculationConfiguration, WindowedCalculationConfigurationCommand>>());
    
            return services;
        });

        Configuration(builder =>
        {

            builder.Services.Configure<CalculationCacheOptions>(builder.Configuration.GetSection("CalculationCache"));
    
                    return builder;
});

        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {
            builder.Services.TryAddSingleton<ICalculationInputResolver, DefaultCalculationInputResolver>();
            builder.Services.TryAddSingleton<ICalculationStepExecutor, CalculationStepExecutor>();
            builder.Services.TryAddSingleton<ICalculationEntityService, CalculationEntityService>();
            builder.Services.TryAddSingleton<ICalculationCatalogProvider, CalculationCatalogProvider>();
            builder.Services.TryAddSingleton<ICalculationEntityProvider, DefaultCalculationEntityProvider>();
            builder.Services.TryAddSingleton<CacheKeyGenerator>();
            builder.Services.TryAddSingleton<ICalculationCacheService, CalculationCacheService>();

            builder.Services.TryAddSingleton<CalculationConfigurationProvider>(sp =>
                new CalculationConfigurationProvider(
                    sp.GetService<ILogger<CalculationConfigurationProvider>>()!,
                    sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                    invalidator: new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));
            builder.Services.TryAddSingleton<DefaultConfigurationProvider<CalculationEntityConfiguration, CalculationEntityConfigurationCommand>>(
                sp => sp.GetRequiredService<CalculationConfigurationProvider>());
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<CalculationEntityConfiguration>>(
                sp => sp.GetRequiredService<CalculationConfigurationProvider>());

            // Why: the polymorphic typed body (Formula/Windowed) is composed by the keystone base dictionary —
            // register one plain DefaultConfigurationProvider per typed body so RegisterFactory can attach it
            // to the header provider via RegisterTypedProvider (read dispatch on ServiceOptionType).
            RegisterTypedBodyProvider<FormulaCalculationConfiguration, FormulaCalculationConfigurationCommand>(builder.Services);
            RegisterTypedBodyProvider<WindowedCalculationConfiguration, WindowedCalculationConfigurationCommand>(builder.Services);
            return builder;
        });

    }

    /// <inheritdoc />
    // Why: the calculation domain registers the services + provider it depends on, instead of the
    // entry-point app. Replaces AddFrameworkCalculationEntities and the ad-hoc Program.cs cluster.
    // "ConfigurationDb"/"calc" are the config-store location the provider targets.

    // Why: builds + registers a plain keystone provider for one typed-body table (cfg-tier; loaded from
    // ConfigurationDb at runtime). Generic so Formula/Windowed share one construction path.
    private static void RegisterTypedBodyProvider<TConfig, TCommand>(IServiceCollection services)
        where TConfig : class, IGenericConfiguration
        where TCommand : ConfigurationCommandBase<TConfig>
    {
        services.AddOptions<List<TConfig>>();
        services.TryAddSingleton<DefaultConfigurationProvider<TConfig, TCommand>>(sp =>
            new DefaultConfigurationProvider<TConfig, TCommand>(
                sp.GetService<ILogger<DefaultConfigurationProvider<TConfig, TCommand>>>(),
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                "ConfigurationDb",
                "calc",
                new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));
    }
}
