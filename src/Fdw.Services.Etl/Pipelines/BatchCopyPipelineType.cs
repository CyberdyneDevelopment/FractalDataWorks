using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
[ServiceTypeOption(typeof(EtlPipelineTypes), "BatchCopy")]
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
            var provider = services.GetRequiredService<IEtlPipelineProvider>();
            var log = loggerFactory?.CreateLogger<BatchCopyPipelineType>() ?? NullLogger<BatchCopyPipelineType>.Instance;

            // Resolve factory from DI (registered in Phase 1)
            var factory = services.GetRequiredService<IBatchCopyPipelineFactory>();

            // Register factory instance with provider
            var factoryResult = provider.Register(Name, factory);
            if (!factoryResult.IsSuccess)
            {
                ServiceTypeLog.OptionFactoryRegistrationFailed(
                    log,
                    nameof(BatchCopyPipelineType),
                    Name,
                    nameof(IBatchCopyPipelineFactory),
                    factoryResult.CurrentMessage);
                return GenericResult<IHost>.Success(host);
            }

            ServiceTypeLog.OptionFactoryRegistered(
                log,
                nameof(BatchCopyPipelineType),
                Name,
                nameof(IBatchCopyPipelineFactory));

            var configProvider = services.GetRequiredService<ImplementationConfigurationProviderBase<BatchCopyPipelineConfiguration, BatchCopyPipelineConfigurationCommand>>();

            var etlKindProvider = services.GetRequiredService<EtlPipelineConfigurationProvider>();
            etlKindProvider.Register(Name, configProvider);

            var survivor = services.GetRequiredService<PipelineServiceConfigurationProvider>();
            survivor.Register("Etl", etlKindProvider);
    
            return GenericResult<IHost>.Success(host);
        });

        Configuration(builder =>
        {

    
                    return GenericResult<IHostApplicationBuilder>.Success(builder);
});

        Registration((builder, loggerFactory) =>
        {



            // Factory - DI handles all constructor dependencies
            builder.Services.AddScoped<IBatchCopyPipelineFactory>(sp => new BatchCopyPipelineFactory(
                sp.GetRequiredService<ILogger<BatchCopyPipelineFactory>>(),
                sp.GetRequiredService<ILoggerFactory>(),
                sp.GetService<IDataGatewayProvider>(),
                sp.GetService<IConnectionProvider>(),
                sp.GetService<IDataStoreProvider>()));

            builder.Services.AddSingleton(sp => new ImplementationConfigurationProviderBase<BatchCopyPipelineConfiguration, BatchCopyPipelineConfigurationCommand>(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<ImplementationConfigurationProviderBase<BatchCopyPipelineConfiguration, BatchCopyPipelineConfigurationCommand>>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                DataStore,
                PathName));

            EtlPipelineTypes.RegisterPipelineExecutionQueue(builder.Services);
            EtlPipelineTypes.RegisterAdditionalServices(builder.Services);
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
