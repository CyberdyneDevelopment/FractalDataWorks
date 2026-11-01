using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Fdw.Services.Notifications.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Fdw.Collections;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Abstractions;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Configuration;
using Fdw.Services.Connections.Abstractions;
using Fdw.ServiceTypes;
using Fdw.ServiceTypes.Logging;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.EtlGateway;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Graphs.Commands;
using Fdw.Services.Pipelines;
using Fdw.Results;
using Fdw.Services.Settings;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// Service type definition for graph pipelines (FDW-1396): components wired by named input and output nodes.
/// </summary>
/// <remarks>
/// A graph pipeline is stored as a <see cref="GraphPipelineConfiguration"/> over <c>pipe.GraphPipelineConfiguration</c> with its
/// components, links and dropped outputs, and run by the row-flow engine. The type registers its factory, its provider and its
/// configuration provider the way the other pipeline types do, and registers its provider with the pipeline domain under its own
/// name.
/// </remarks>
[Implementation(typeof(EtlPipelineTypes), "Graph")]
public sealed class GraphPipelineType : EtlPipelineTypeBase<IEtlPipeline, IGraphPipelineFactory, GraphPipelineConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GraphPipelineType"/> class.
    /// Instance is created by source generator in EtlPipelineTypes collection.
    /// </summary>
    public GraphPipelineType() : base(
        name: "Graph",
        sectionName: "Graph",
        displayName: "Graph",
        description: "Graph pipeline: components wired by named input and output nodes, run by the row-flow engine",
        category: "ETL",
        defaultContainerName: "GraphPipelineConfiguration")
    {
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;

            // One domain, one implementation: the engine's provider registers under its own
            // name on the Pipeline domain provider. There is no intermediate kind row to
            // mint or resolve -- pipe.Pipeline already names the implementation.
            services.GetRequiredService<PipelineServiceConfigurationProvider>()
                .Register(Name, services.GetRequiredService<GraphPipelineConfigurationProvider>());
    
            return GenericResult<IHost>.Success(host);
        });

        Configuration(builder =>
        {

    
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {



            // Factory - DI handles all constructor dependencies
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IGraphPipelineFactory),
                sp => new GraphPipelineFactory(
                    sp.GetRequiredService<ILogger<GraphPipelineFactory>>(),
                    sp.GetRequiredService<ILoggerFactory>(),
                    sp.GetRequiredService<IFlowExecutor>(),
                    sp.GetService<IDataGatewayProvider>(),
                    sp.GetService<IConnectionProvider>(),
                    sp.GetService<IDataStoreProvider>(),
                    sp.GetService<IDataSetConfigurationProvider>(),
                    sp.GetService<IConnectionConfigurationProvider>(),
                    () => sp.GetRequiredService<IPipelineRunStarter>(),
                    sp.GetService<IEtlGateway>(),
                    sp.GetService<IRuleNotifier>(),
                    sp.GetService<ITransformationConfigurationProvider>(),
                    sp.GetService<IEffectiveSettingsProvider>(),
                    sp.GetService<IExecutionTracker>()),
                ServiceLifetime.Scoped));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IGraphPipelineProvider),
                sp => new GraphPipelineProvider(sp.GetRequiredService<IGraphPipelineFactory>()),
                ServiceLifetime.Scoped));

            builder.Services.TryAddSingleton(sp => new GraphPipelineConfigurationProvider(
                sp.GetRequiredService<ILogger<GraphPipelineConfigurationProvider>>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                EtlPipelineTypes.ConfigurationConnection, PathName, Container));

            EtlPipelineTypes.RegisterPipelineExecutionQueue(builder.Services);
            EtlPipelineTypes.RegisterAdditionalServices(builder.Services);

            // Decorate the domain's own AddScoped<IEtlPipelineProvider> registration -- already in
            // builder.Services by the time this runs, since EtlPipelineTypes' own Registration body
            // sets it up before running the option collect.
            var existing = builder.Services.Single(d => d.ServiceType == typeof(IEtlPipelineProvider));
            builder.Services.Remove(existing);
            builder.Services.AddScoped<IEtlPipelineProvider>(sp =>
            {
                var provider = (IEtlPipelineProvider)existing.ImplementationFactory!(sp);

                var factoryResult = provider.Register(Name, () => sp.GetRequiredService<IGraphPipelineProvider>());
                if (!factoryResult.IsSuccess)
                {
                    ServiceTypeLog.OptionFactoryRegistrationFailed(
                        sp.GetService<ILoggerFactory>()?.CreateLogger<GraphPipelineType>() ?? NullLogger<GraphPipelineType>.Instance,
                        nameof(GraphPipelineType), Name, nameof(IGraphPipelineFactory), factoryResult.CurrentMessage);
                }

                return provider;
            });

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
