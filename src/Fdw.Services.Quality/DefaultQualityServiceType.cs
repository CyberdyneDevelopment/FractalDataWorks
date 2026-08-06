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
    
                    return builder;
});

        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {

            builder.Services.TryAddSingleton<QualityConfigurationProvider>(sp =>
                new QualityConfigurationProvider(
                    sp.GetService<ILogger<QualityConfigurationProvider>>() ?? NullLogger<QualityConfigurationProvider>.Instance,
                    sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                    invalidator: new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));

            builder.Services.TryAddScoped<IQualityService, QualityService>();
            builder.Services.TryAddScoped<ICatalogService, CatalogService>();
            // Why: PromotionService keeps an in-memory request list (stub before DB-backed
            // persistence lands). Scoped meant the list reset per HTTP request, so Create
            // → Get always 404'd. Singleton keeps the list alive across requests until a
            // real DataGateway-backed implementation replaces it.
            builder.Services.TryAddSingleton<IPromotionService, PromotionService>();
            return builder;
        });

    }

}
