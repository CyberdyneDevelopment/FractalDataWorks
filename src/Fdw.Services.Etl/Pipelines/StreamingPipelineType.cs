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
        // Why Initialize and not Register: this wiring needs a LIVE container (it resolves the
        // domain provider and its typed-body providers), and Register runs while the container
        // is still being built. Initialize runs after Build() with a real IServiceProvider.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var provider = services.GetRequiredService<IPlatformServiceProvider<IEtlPipeline, PipelineConfiguration>>();
            var log = loggerFactory?.CreateLogger<StreamingPipelineType>() ?? NullLogger<StreamingPipelineType>.Instance;

            // Resolve factory from DI (registered in Phase 1)
            var factory = services.GetRequiredService<IStreamingPipelineFactory>();

            // Register factory instance with provider
            var factoryResult = provider.Register(Name, factory);
            if (!factoryResult.IsSuccess)
            {
                // Why this exit is logged at Error: it returns SUCCESS to the host. Without a line
                // here this engine simply is not creatable and the host starts as though it were —
                // the gap surfaces only at the first pipeline run, far from the method that caused it.
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

            // Why: Resolve from DI — provider was registered with Lazy<IConfigurationGateway> in the option's Register phase.
            // Not registered with the runtime IPlatformServiceProvider (typed to the ETL-kind EtlPipelineConfiguration,
            // resolved from DI by the generated resolver) — the engine config is a distinct typed body reached
            // through the kind body's .Configuration, attached below.
            var configProvider = services.GetRequiredService<DefaultConfigurationProvider<StreamingPipelineConfiguration, StreamingPipelineConfigurationCommand>>();

            // Why: wire the two-level configuration typed-body chain (Pipeline → Etl → engine). (1) Attach this
            // engine's body provider to the ETL-kind provider keyed by the engine discriminator ("Streaming");
            // (2) attach the ETL-kind provider to the general header keyed by the KIND discriminator
            // ("Etl"). Both are idempotent and run against the real singletons. See BatchCopyPipelineType.
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

            // Why: register the ETL-kind header provider (idempotent TryAdd) that EtlPipelineTypes' generated
            // provider resolves as IServiceConfigurationProvider<EtlPipelineConfiguration> — the domain,
            // not the app, registers what it depends on.

            // Why: ETL is a KIND the general Pipeline header consumes. Register the general header provider
            // (idempotent TryAdd) here too so the "Etl" typed-provider attachment in RegisterFactory never
            // depends on cross-collection registration ordering (PipelineServiceTypes vs EtlPipelineTypes).
            PipelineServiceConfigurationProvider.RegisterDomainConfiguration(builder.Services);

            // Factory - DI handles all constructor dependencies
            // Why Scoped: the factory optionally consumes IDataGateway (scoped). EtlPipelineTypes'
            // generated IPlatformServiceProvider<IEtlPipeline, PipelineConfiguration> is itself Scoped and
            // resolves this factory via RegisterFactory inside its own per-scope resolver, so a Scoped
            // factory here is lifetime-consistent, not a captive dependency.
            // Why: lambda registration so the cross-collection connection provider is injected as a Lazy —
            // the factory must stay pure (FDW045). The Lazy defers resolution to pipeline-build time; .Value
            // yields the provider or null, preserving the optional semantics.
            builder.Services.AddScoped<IStreamingPipelineFactory>(sp => new StreamingPipelineFactory(
                sp.GetRequiredService<ILogger<StreamingPipelineFactory>>(),
                sp.GetRequiredService<ILoggerFactory>(),
                sp.GetService<IDataGateway>(),
                new Lazy<IPlatformServiceProvider<IGenericConnection, IGenericConfiguration>>(
                    () => sp.GetService<IPlatformServiceProvider<IGenericConnection, IGenericConfiguration>>()!)));

            // Why: Lazy<IConfigurationGateway> defers cfg resolution until first runtime query, avoiding
            // circular dependency with the DataGateway that hasn't been built yet at registration time.
            // DataStore flows from TypeCollection.Configure() so "ConfigurationDb" is never hardcoded here.
            builder.Services.AddSingleton(sp => new DefaultConfigurationProvider<StreamingPipelineConfiguration, StreamingPipelineConfigurationCommand>(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<DefaultConfigurationProvider<StreamingPipelineConfiguration, StreamingPipelineConfigurationCommand>>(),
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                DataStore,
                PathName));

            // Why: a pipeline type can't run without the execution queue + its background consumer, so the
            // option registers what it needs (idempotent — guarded by an existing-registration check, so
            // BatchCopy and Streaming can both call it and the first wins). This is the option-owned home
            // for the executor: any host with any pipeline type gets it automatically, with no host-level
            // host-level registration call and no OrchestrationTypes hand-wiring.
            EtlPipelineTypes.RegisterPipelineExecutionQueue(builder.Services);
            // Why: the inspection/test-control singletons the execution subsystem exposes
            // (IPipelineExecutionInspector, IPipelineTestController) are needed wherever a pipeline
            // can run, so the option that brings the pipeline registers them too. Idempotent TryAdd,
            // so both pipeline options calling it is harmless.
            EtlPipelineTypes.RegisterAdditionalServices(builder.Services);
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

    }

}
