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
/// Service type definition for streaming pipelines.
/// Provides metadata, factory creation, and configuration binding for continuous streaming ETL operations.
/// </summary>
/// <remarks>
/// <para>
/// Streaming pipelines process data continuously with configurable buffering and rate limiting.
/// Configuration is loaded from "Pipelines:{PipelineName}" sections:
/// <code>
/// {
///   "Pipelines": {
///     "RealtimeSync": {
///       "PipelineType": "Streaming",
///       "SourceConnectionName": "KafkaSource",
///       "DestinationConnectionName": "TargetDb",
///       "BufferSize": 1000,
///       "FlushIntervalMs": 5000,
///       "MaxRecordsPerSecond": 10000
///     }
///   }
/// }
/// </code>
/// </para>
/// </remarks>
[ServiceTypeOption(typeof(EtlPipelineTypes), "Streaming")]
public sealed class StreamingPipelineType : EtlPipelineTypeBase<IEtlPipeline, IStreamingPipelineFactory, StreamingPipelineConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StreamingPipelineType"/> class.
    /// Instance is created by source generator in EtlPipelineTypes collection.
    /// </summary>
    public StreamingPipelineType() : base(
        name: "Streaming",
        sectionName: "Streaming",
        displayName: "Streaming",
        description: "Streaming pipeline for continuous ETL operations with buffering and rate limiting",
        category: "ETL",
        defaultContainerName: "StreamingPipeline")
    {
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var provider = services.GetRequiredService<IEtlPipelineProvider>();
            var log = loggerFactory?.CreateLogger<StreamingPipelineType>() ?? NullLogger<StreamingPipelineType>.Instance;

            // Resolve factory from DI (registered in Phase 1)
            var factory = services.GetRequiredService<IStreamingPipelineFactory>();

            // Register factory instance with provider
            var factoryResult = provider.Register(Name, factory);
            if (!factoryResult.IsSuccess)
            {
                ServiceTypeLog.OptionFactoryRegistrationFailed(
                    log,
                    nameof(StreamingPipelineType),
                    Name,
                    nameof(IStreamingPipelineFactory),
                    factoryResult.CurrentMessage);
                return GenericResult<IHost>.Success(host);
            }

            ServiceTypeLog.OptionFactoryRegistered(
                log,
                nameof(StreamingPipelineType),
                Name,
                nameof(IStreamingPipelineFactory));

            var configProvider = services.GetRequiredService<ImplementationConfigurationProviderBase<StreamingPipelineConfiguration, StreamingPipelineConfigurationCommand>>();

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
            builder.Services.AddScoped<IStreamingPipelineFactory>(sp => new StreamingPipelineFactory(
                sp.GetRequiredService<ILogger<StreamingPipelineFactory>>(),
                sp.GetRequiredService<ILoggerFactory>(),
                sp.GetService<IDataGateway>(),
                sp.GetService<IConnectionProvider>()));

            builder.Services.AddSingleton(sp => new ImplementationConfigurationProviderBase<StreamingPipelineConfiguration, StreamingPipelineConfigurationCommand>(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<ImplementationConfigurationProviderBase<StreamingPipelineConfiguration, StreamingPipelineConfigurationCommand>>(),
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                DataStore,
                PathName));

            EtlPipelineTypes.RegisterPipelineExecutionQueue(builder.Services);
            EtlPipelineTypes.RegisterAdditionalServices(builder.Services);
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
