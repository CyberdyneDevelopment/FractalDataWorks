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
[Implementation(typeof(QualityServiceTypes), "Default")]
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

            builder.Services.Configure<List<QualityRuleImplementationConfiguration>>(builder.Configuration.GetSection("Quality:QualityRule"));
            builder.Services.Configure<List<DataSetAnnotationImplementationConfiguration>>(builder.Configuration.GetSection("Catalog:DataSetAnnotation"));
            builder.Services.Configure<List<GlossaryTermImplementationConfiguration>>(builder.Configuration.GetSection("Catalog:GlossaryTerm"));
    
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAddSingleton<GlossaryTermImplementationConfigurationProvider>();
            builder.Services.TryAddSingleton<IGlossaryTermImplementationConfigurationProvider>(sp => sp.GetRequiredService<GlossaryTermImplementationConfigurationProvider>());
            builder.Services.TryAddSingleton<GlossaryTermConfigurationProvider>(sp =>
            {
                var domain = new GlossaryTermConfigurationProvider(
                    sp.GetRequiredService<ILogger<GlossaryTermConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>());
                domain.Register("GlossaryTerm", sp.GetRequiredService<IGlossaryTermImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IGlossaryTermConfigurationProvider>(sp => sp.GetRequiredService<GlossaryTermConfigurationProvider>());

            builder.Services.TryAddSingleton<DataSetAnnotationImplementationConfigurationProvider>();
            builder.Services.TryAddSingleton<IDataSetAnnotationImplementationConfigurationProvider>(sp => sp.GetRequiredService<DataSetAnnotationImplementationConfigurationProvider>());
            builder.Services.TryAddSingleton<DataSetAnnotationConfigurationProvider>(sp =>
            {
                var domain = new DataSetAnnotationConfigurationProvider(
                    sp.GetRequiredService<ILogger<DataSetAnnotationConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>());
                domain.Register("DataSetAnnotation", sp.GetRequiredService<IDataSetAnnotationImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IDataSetAnnotationConfigurationProvider>(sp => sp.GetRequiredService<DataSetAnnotationConfigurationProvider>());

            builder.Services.TryAddSingleton<PromotionRequestImplementationConfigurationProvider>();
            builder.Services.TryAddSingleton<IPromotionRequestImplementationConfigurationProvider>(sp => sp.GetRequiredService<PromotionRequestImplementationConfigurationProvider>());
            builder.Services.TryAddSingleton<PromotionRequestConfigurationProvider>(sp =>
            {
                var domain = new PromotionRequestConfigurationProvider(
                    sp.GetRequiredService<ILogger<PromotionRequestConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>());
                domain.Register("PromotionRequest", sp.GetRequiredService<IPromotionRequestImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IPromotionRequestConfigurationProvider>(sp => sp.GetRequiredService<PromotionRequestConfigurationProvider>());

            builder.Services.TryAddSingleton<EnvironmentImplementationConfigurationProvider>();
            builder.Services.TryAddSingleton<IEnvironmentImplementationConfigurationProvider>(sp => sp.GetRequiredService<EnvironmentImplementationConfigurationProvider>());
            builder.Services.TryAddSingleton<EnvironmentConfigurationProvider>(sp =>
            {
                var domain = new EnvironmentConfigurationProvider(
                    sp.GetRequiredService<ILogger<EnvironmentConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>());
                domain.Register("Environment", sp.GetRequiredService<IEnvironmentImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IEnvironmentConfigurationProvider>(sp => sp.GetRequiredService<EnvironmentConfigurationProvider>());

            builder.Services.TryAddSingleton<QualityRuleImplementationConfigurationProvider>();
            builder.Services.TryAddSingleton<IQualityRuleImplementationConfigurationProvider>(sp => sp.GetRequiredService<QualityRuleImplementationConfigurationProvider>());
            builder.Services.TryAddSingleton<QualityRuleConfigurationProvider>(sp =>
            {
                var domain = new QualityRuleConfigurationProvider(
                    sp.GetRequiredService<ILogger<QualityRuleConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>());
                domain.Register("QualityRule", sp.GetRequiredService<IQualityRuleImplementationConfigurationProvider>());
                return domain;
            });
            builder.Services.TryAddSingleton<IQualityRuleConfigurationProvider>(sp => sp.GetRequiredService<QualityRuleConfigurationProvider>());


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
