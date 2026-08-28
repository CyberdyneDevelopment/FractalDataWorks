using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Quality.Configuration;
using Fdw.Services.Quality.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Fdw.Results;

namespace Fdw.Services.Quality;

/// <summary>
/// Default quality service type that registers quality, catalog, and promotion services
/// with the dependency injection container.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(QualityServiceTypes), "Default")]
public sealed class DefaultQualityServiceType : QualityServiceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultQualityServiceType"/> class.
    /// </summary>
    public DefaultQualityServiceType()
        : base(
            "Default",
            "Quality:Default",
            "Default Quality Services",
            "Default quality, catalog, and promotion services")
    {
        Configuration(builder =>
        {

            builder.Services.Configure<List<QualityRuleConfiguration>>(builder.Configuration.GetSection("Quality:QualityRule"));
            builder.Services.Configure<List<DataSetAnnotationConfiguration>>(builder.Configuration.GetSection("Catalog:DataSetAnnotation"));
            builder.Services.Configure<List<GlossaryTermConfiguration>>(builder.Configuration.GetSection("Catalog:GlossaryTerm"));
    
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {

            builder.Services.TryAddSingleton<QualityConfigurationProvider>(sp =>
                new QualityConfigurationProvider(
                    sp.GetService<ILogger<QualityConfigurationProvider>>() ?? NullLogger<QualityConfigurationProvider>.Instance,
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                        QualityServiceTypes.ConfigurationConnection));

            builder.Services.TryAddScoped<IQualityService, QualityService>();
            builder.Services.TryAddScoped<ICatalogService, CatalogService>();
            builder.Services.TryAddSingleton<IPromotionService, PromotionService>();
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
