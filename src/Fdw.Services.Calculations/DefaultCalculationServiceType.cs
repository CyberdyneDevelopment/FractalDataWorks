using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Services.Calculations.Abstractions;
using Fdw.Services.Calculations.Abstractions.CalculationSources;
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
/// the gateway-backed CalculationConfigurationProvider.
/// </summary>
[ExcludeFromCodeCoverage]
[Implementation(typeof(CalculationServiceTypes), "Default")]
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
            var domain = services.GetRequiredService<ICalculationConfigurationProvider>();
            domain.Register("Formula", services.GetRequiredService<IFormulaCalculationConfigurationProvider>());
            domain.Register("Windowed", services.GetRequiredService<IWindowedCalculationConfigurationProvider>());
    
            return GenericResult<IHost>.Success(host);
        });

        Registration((builder, loggerFactory) =>
        {

            builder.Services.TryAddSingleton<ICalculationInputResolver, DefaultCalculationInputResolver>();
            builder.Services.TryAddSingleton<ICalculationStepExecutor, CalculationStepExecutor>();
            builder.Services.TryAddSingleton<ICalculationEntityService, CalculationEntityService>();
            builder.Services.TryAddSingleton<ICalculationCatalogProvider, CalculationCatalogProvider>();
            builder.Services.TryAddSingleton<ICalculationEntityProvider, DefaultCalculationEntityProvider>();

            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddSingleton<ICalculationConfigurationProvider, CalculationConfigurationProvider>(sp => new CalculationConfigurationProvider(sp.GetRequiredService<ILogger<CalculationConfigurationProvider>>(), sp.GetRequiredService<IConfigurationGatewayProvider>(), CalculationServiceTypes.ConfigurationConnection));

            builder.Services.AddSingleton<IFormulaCalculationConfigurationProvider, FormulaCalculationConfigurationProvider>(sp => new FormulaCalculationConfigurationProvider(sp.GetRequiredService<ILogger<FormulaCalculationConfigurationProvider>>(), sp.GetRequiredService<IConfigurationGatewayProvider>(), CalculationServiceTypes.ConfigurationConnection));
            builder.Services.AddSingleton<IWindowedCalculationConfigurationProvider, WindowedCalculationConfigurationProvider>(sp => new WindowedCalculationConfigurationProvider(sp.GetRequiredService<ILogger<WindowedCalculationConfigurationProvider>>(), sp.GetRequiredService<IConfigurationGatewayProvider>(), CalculationServiceTypes.ConfigurationConnection));
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }
}
