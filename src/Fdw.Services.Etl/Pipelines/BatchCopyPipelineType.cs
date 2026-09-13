using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Fdw.Collections;
using Fdw.Data.Abstractions;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Services.Connections.Abstractions;
using Fdw.ServiceTypes;
using Fdw.ServiceTypes.Logging;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Pipelines.Commands;
using Fdw.Services.Pipelines;
using Fdw.Results;

namespace Fdw.Services.Etl.Pipelines;

/// <summary>
/// Service type definition for batch copy pipelines.
/// Provides metadata, factory creation, and configuration binding for batch copy ETL operations.
/// </summary>
/// <remarks>
/// <para>
/// Batch copy pipelines extract data in batches, apply transforms, and load to a destination.
/// Configuration is loaded from "Pipelines:{PipelineName}" sections:
/// <code>
/// {
///   "Pipelines": {
///     "OrdersSync": {
///       "PipelineType": "BatchCopy",
///       "SourceConnectionName": "SourceDb",
///       "DestinationConnectionName": "TargetDb",
///       "MaxParallelism": 4,
///       "LoadMode": "Append"
///     }
///   }
/// }
/// </code>
/// </para>
/// </remarks>
[Implementation(typeof(EtlPipelineTypes), "BatchCopy")]
public sealed class BatchCopyPipelineType : EtlPipelineTypeBase<IEtlPipeline, IBatchCopyPipelineFactory, BatchCopyPipelineConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BatchCopyPipelineType"/> class.
    /// Instance is created by source generator in EtlPipelineTypes collection.
    /// </summary>
    public BatchCopyPipelineType() : base(
        name: "BatchCopy",
        sectionName: "BatchCopy",
        displayName: "Batch Copy",
        description: "Batch copy pipeline for ETL operations with configurable parallelism",
        category: "ETL",
        defaultContainerName: "BatchCopyPipeline")
    {
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;

            var etlKindProvider = services.GetRequiredService<EtlPipelineConfigurationProvider>();
            etlKindProvider.Register(Name, services.GetRequiredService<BatchCopyPipelineConfigurationProvider>());

            services.GetRequiredService<PipelineServiceConfigurationProvider>().Register("Etl", etlKindProvider);
    
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
                typeof(IBatchCopyPipelineFactory),
                sp => new BatchCopyPipelineFactory(
                sp.GetRequiredService<ILogger<BatchCopyPipelineFactory>>(),
                sp.GetRequiredService<ILoggerFactory>(),
                sp.GetService<IDataGatewayProvider>(),
                sp.GetService<IConnectionProvider>(),
                sp.GetService<IDataStoreProvider>()),
                ServiceLifetime.Scoped));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IBatchCopyPipelineProvider),
                sp => new BatchCopyPipelineProvider(sp.GetRequiredService<IBatchCopyPipelineFactory>()),
                ServiceLifetime.Scoped));

            builder.Services.TryAddSingleton(sp => new BatchCopyPipelineConfigurationProvider(
                sp.GetRequiredService<ILogger<BatchCopyPipelineConfigurationProvider>>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                EtlPipelineTypes.ConfigurationConnection));

            EtlPipelineTypes.RegisterPipelineExecutionQueue(builder.Services);
            EtlPipelineTypes.RegisterAdditionalServices(builder.Services);
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

    /// <inheritdoc/>
    /// <remarks>
    /// Why this replaces what used to be here in <c>Initialization</c>: that callback resolved
    /// <see cref="IEtlPipelineProvider"/> from the ROOT container once, at startup, and this
    /// domain provider is registered <c>AddScoped</c> — so root's copy is one instance among many
    /// a real request never sees, and a real request's own instance never had this call reach it.
    /// This method is instead called once per construction, by <c>EtlPipelineTypes</c>'s own
    /// factory, and handed THAT construction's <paramref name="serviceProvider"/> — the same
    /// scope root or a request actually used to build <paramref name="domainProvider"/> itself.
    /// </remarks>
    public override IGenericResult RegisterImplementationProvider(IEtlPipelineProvider domainProvider, IServiceProvider serviceProvider, ILogger logger)
    {
        var factoryResult = domainProvider.Register(Name, () => serviceProvider.GetRequiredService<IBatchCopyPipelineProvider>());
        if (!factoryResult.IsSuccess)
        {
            ServiceTypeLog.OptionFactoryRegistrationFailed(logger, nameof(BatchCopyPipelineType), Name, nameof(IBatchCopyPipelineFactory), factoryResult.CurrentMessage);
            return factoryResult;
        }

        ServiceTypeLog.OptionFactoryRegistered(logger, nameof(BatchCopyPipelineType), Name, nameof(IBatchCopyPipelineFactory));
        return factoryResult;
    }

}
