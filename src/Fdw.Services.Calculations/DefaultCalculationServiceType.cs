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
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var header = services.GetRequiredService<CalculationConfigurationProvider>();

            header.Register(
                "Formula",
                services.GetRequiredService<ImplementationConfigurationProviderBase<FormulaCalculationConfiguration, FormulaCalculationConfigurationCommand>>());

            header.Register(
                "Windowed",
                services.GetRequiredService<ImplementationConfigurationProviderBase<WindowedCalculationConfiguration, WindowedCalculationConfigurationCommand>>());
    
            return GenericResult<IHost>.Success(host);
        });

        Configuration(builder =>
        {

            builder.Services.Configure<CalculationCacheOptions>(builder.Configuration.GetSection("CalculationCache"));
    
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAddSingleton<ICalculationInputResolver, DefaultCalculationInputResolver>();
            builder.Services.TryAddSingleton<ICalculationStepExecutor, CalculationStepExecutor>();
            builder.Services.TryAddSingleton<ICalculationEntityService, CalculationEntityService>();
            builder.Services.TryAddSingleton<ICalculationCatalogProvider, CalculationCatalogProvider>();
            builder.Services.TryAddSingleton<ICalculationEntityProvider, DefaultCalculationEntityProvider>();
            builder.Services.TryAddSingleton<CacheKeyGenerator>();

            builder.Services.AddDistributedMemoryCache();
            builder.Services.TryAddSingleton<ICalculationCacheService, CalculationCacheService>();

            builder.Services.TryAddSingleton<CalculationConfigurationProvider>(sp =>
                new CalculationConfigurationProvider(
                    sp.GetService<ILogger<CalculationConfigurationProvider>>()!,
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                        CalculationServiceTypes.ConfigurationConnection));
            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<CalculationEntityConfiguration, CalculationEntityConfigurationCommand>>(
                sp => sp.GetRequiredService<CalculationConfigurationProvider>());
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<CalculationEntityConfiguration>>(
                sp => sp.GetRequiredService<CalculationConfigurationProvider>());

            RegisterTypedBodyProvider<FormulaCalculationConfiguration, FormulaCalculationConfigurationCommand>(builder.Services);
            RegisterTypedBodyProvider<WindowedCalculationConfiguration, WindowedCalculationConfigurationCommand>(builder.Services);
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

    /// <inheritdoc />

    private static void RegisterTypedBodyProvider<TConfig, TCommand>(IServiceCollection services)
        where TConfig : class, IGenericConfiguration
        where TCommand : ConfigurationCommandBase<TConfig>
    {
        services.AddOptions<List<TConfig>>();
        services.TryAddSingleton<ImplementationConfigurationProviderBase<TConfig, TCommand>>(sp =>
            new ImplementationConfigurationProviderBase<TConfig, TCommand>(
                sp.GetService<ILogger<ImplementationConfigurationProviderBase<TConfig, TCommand>>>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                "PlatformConfiguration",
                "calc"));
    }
}
