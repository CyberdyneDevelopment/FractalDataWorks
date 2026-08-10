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
using Fdw.Results;

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
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var header = services.GetRequiredService<CalculationConfigurationProvider>();

            header.Register(
                "Formula",
                services.GetRequiredService<DefaultConfigurationProvider<FormulaCalculationConfiguration, FormulaCalculationConfigurationCommand>>());

            header.Register(
                "Windowed",
                services.GetRequiredService<DefaultConfigurationProvider<WindowedCalculationConfiguration, WindowedCalculationConfigurationCommand>>());
    
            return GenericResult<IHost>.Success(host);
        });

        Configuration(builder =>
        {

            builder.Services.Configure<CalculationCacheOptions>(builder.Configuration.GetSection("CalculationCache"));
    
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {
            builder.Services.TryAddSingleton<ICalculationInputResolver, DefaultCalculationInputResolver>();
            builder.Services.TryAddSingleton<ICalculationStepExecutor, CalculationStepExecutor>();
            builder.Services.TryAddSingleton<ICalculationEntityService, CalculationEntityService>();
            builder.Services.TryAddSingleton<ICalculationCatalogProvider, CalculationCatalogProvider>();
            builder.Services.TryAddSingleton<ICalculationEntityProvider, DefaultCalculationEntityProvider>();
            builder.Services.TryAddSingleton<CacheKeyGenerator>();

            // Why the cache store is registered here: CalculationCacheService takes IDistributedCache
            // as a required constructor dependency, so the option that registers the service is the
            // place that can state what it needs. Left to the host it is an AddDistributedMemoryCache()
            // call in Program.cs that looks like generic framework wiring and gives no hint which
            // service fails without it — the failure surfaces as an unresolvable ICalculationCacheService
            // at first use, one layer away from the missing registration.
            //
            // AddDistributedMemoryCache is the in-process implementation, and TryAdd semantics mean a
            // host that wants a real distributed store registers it before this phase runs and keeps it;
            // an application replacing this phase body via Registration(...) does the same. So this is a
            // default that a deployment can outgrow without editing the option.
            builder.Services.AddDistributedMemoryCache();
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
            // to the header provider via Register (read dispatch on ServiceOptionType).
            RegisterTypedBodyProvider<FormulaCalculationConfiguration, FormulaCalculationConfigurationCommand>(builder.Services);
            RegisterTypedBodyProvider<WindowedCalculationConfiguration, WindowedCalculationConfigurationCommand>(builder.Services);
            return GenericResult<IHostApplicationBuilder>.Success(builder);
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
