using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Configuration;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Commands;
using Fdw.ServiceTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines;

/// <summary>
/// ServiceTypeCollection for the pipeline-service domain (gateway-backed pipeline
/// configuration provider). Distinct from the EtlPipelineTypes engine collection.
///
/// Why discovery is not restricted to this compilation: the orchestration domain
/// (Fdw.Services.Etl.Projects) composes pipelines and so sits above this package, and its option
/// is declared there because only that assembly can name the types it registers. Restricting
/// discovery here would drop that option silently -- it would compile, register nothing, and give
/// no indication why. Cross-assembly options are the norm for a collection others extend;
/// ConnectionTypes works the same way.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(PipelineServiceTypeBase),
    typeof(IPipelineServiceType),
    typeof(PipelineServiceTypes),
    ServiceCategory = "PipelineService")]
public partial class PipelineServiceTypes : ServiceTypeCollectionBase<PipelineServiceTypeBase, IPipelineServiceType>
{
    /// <summary>
    /// The connection this domain's configuration rows are read from and written to.
    /// </summary>
    public static string ConfigurationConnection { get; set; } = "PlatformConfiguration";

    /// <summary>
    /// Sets this collection's Register body: the option collect, then this domain's configuration provider.
    /// </summary>
    static PipelineServiceTypes()
    {
        var collectOptions = RegisterFunc;

        Registration((builder, loggerFactory) =>
        {
            var registered = collectOptions(builder, loggerFactory);
            if (registered.IsFailure)
                return registered;

            builder.Services.TryAddSingleton<IPipelineConfigurationProvider>(sp =>
                new PipelineServiceConfigurationProvider(
                    sp.GetService<ILogger<PipelineServiceConfigurationProvider>>(),
                    sp.GetRequiredService<IConfigurationGatewayProvider>(),
                    ConfigurationConnection));
            builder.Services.TryAddSingleton<PipelineServiceConfigurationProvider>(
                sp => (PipelineServiceConfigurationProvider)sp.GetRequiredService<IPipelineConfigurationProvider>());
            builder.Services.TryAddSingleton<ImplementationConfigurationProviderBase<PipelineConfiguration, PipelineConfigurationCommand>>(
                sp => sp.GetRequiredService<PipelineServiceConfigurationProvider>());
            builder.Services.TryAddSingleton<IServiceConfigurationProvider<PipelineConfiguration>>(
                sp => sp.GetRequiredService<PipelineServiceConfigurationProvider>());

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
