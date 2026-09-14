using System;
using System.Collections.Generic;
using System.Linq;
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

            // Decorate the domain's own AddScoped<IEtlPipelineProvider> registration -- already in
            // builder.Services by the time this runs, since EtlPipelineTypes' own Registration body
            // sets it up before running the option collect.
            var existing = builder.Services.Single(d => d.ServiceType == typeof(IEtlPipelineProvider));
            builder.Services.Remove(existing);
            builder.Services.AddScoped<IEtlPipelineProvider>(sp =>
            {
                var provider = (IEtlPipelineProvider)existing.ImplementationFactory!(sp);

                var factoryResult = provider.Register(Name, () => sp.GetRequiredService<IStreamingPipelineProvider>());
                if (!factoryResult.IsSuccess)
                {
                    ServiceTypeLog.OptionFactoryRegistrationFailed(
                        sp.GetService<ILoggerFactory>()?.CreateLogger<StreamingPipelineType>() ?? NullLogger<StreamingPipelineType>.Instance,
                        nameof(StreamingPipelineType), Name, nameof(IStreamingPipelineFactory), factoryResult.CurrentMessage);
                }

                return provider;
            });

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }
}
