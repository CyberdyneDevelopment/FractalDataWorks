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
[Implementation(typeof(EtlPipelineTypes), "Streaming")]
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

            var etlKindProvider = services.GetRequiredService<EtlPipelineConfigurationProvider>();
            etlKindProvider.Register(Name, services.GetRequiredService<StreamingPipelineConfigurationProvider>());

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
                typeof(IStreamingPipelineFactory),
                sp => new StreamingPipelineFactory(
                sp.GetRequiredService<ILogger<StreamingPipelineFactory>>(),
                sp.GetRequiredService<ILoggerFactory>(),
                sp.GetService<IDataGatewayProvider>(),
                sp.GetService<IConnectionProvider>()),
                ServiceLifetime.Scoped));
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(IStreamingPipelineProvider),
                sp => new StreamingPipelineProvider(sp.GetRequiredService<IStreamingPipelineFactory>()),
                ServiceLifetime.Scoped));

            builder.Services.TryAddSingleton(sp => new StreamingPipelineConfigurationProvider(
                sp.GetRequiredService<ILogger<StreamingPipelineConfigurationProvider>>(),
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
        var factoryResult = domainProvider.Register(Name, () => serviceProvider.GetRequiredService<IStreamingPipelineProvider>());
        if (!factoryResult.IsSuccess)
        {
            ServiceTypeLog.OptionFactoryRegistrationFailed(logger, nameof(StreamingPipelineType), Name, nameof(IStreamingPipelineFactory), factoryResult.CurrentMessage);
            return factoryResult;
        }

        ServiceTypeLog.OptionFactoryRegistered(logger, nameof(StreamingPipelineType), Name, nameof(IStreamingPipelineFactory));
        return factoryResult;
    }

}
