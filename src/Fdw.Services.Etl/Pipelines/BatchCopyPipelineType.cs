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
        // Why Initialize and not Register: this wiring needs a LIVE container (it resolves the
        // domain provider and its typed-body providers), and Register runs while the container
        // is still being built. Initialize runs after Build() with a real IServiceProvider.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var provider = services.GetRequiredService<IPlatformServiceProvider<IEtlPipeline, PipelineConfiguration>>();
            var log = loggerFactory?.CreateLogger<BatchCopyPipelineType>() ?? NullLogger<BatchCopyPipelineType>.Instance;

            // Resolve factory from DI (registered in Phase 1)
            var factory = services.GetRequiredService<IBatchCopyPipelineFactory>();

            // Register factory instance with provider
            var factoryResult = provider.Register(Name, factory);
            if (!factoryResult.IsSuccess)
            {
                // Why this exit is logged at Error: it returns SUCCESS to the host. Without a line
                // here this engine simply is not creatable and the host starts as though it were —
                // the gap surfaces only at the first pipeline run, far from the method that caused it.
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

            // Why: Resolve from DI — provider was registered with Lazy<IConfigurationGateway> in the option's Register phase.
            // Not registered with the runtime IPlatformServiceProvider (which is typed to the ETL-kind
            // EtlPipelineConfiguration, resolved from DI by the generated resolver) — the engine config is a
            // distinct typed body reached through the kind body's .Configuration, attached below.
            var configProvider = services.GetRequiredService<DefaultConfigurationProvider<BatchCopyPipelineConfiguration, BatchCopyPipelineConfigurationCommand>>();

            // Why: wire the two-level configuration typed-body chain (Pipeline → Etl → engine), mirroring the
            // Calculation precedent but across packages. (1) Attach this engine's body provider to the ETL-kind
            // provider keyed by the engine discriminator ("BatchCopy"); (2) attach the ETL-kind provider to the
            // general header keyed by the KIND discriminator ("Etl"). Both attachments are idempotent
            // and run against the real singletons (RegisterFactory executes inside the collection's provider
            // resolver against the app sp), so the keystone's ComposeTypedBody recurses Pipeline→Etl→engine.
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
            builder.Services.AddScoped<IBatchCopyPipelineFactory>(sp => new BatchCopyPipelineFactory(
                sp.GetRequiredService<ILogger<BatchCopyPipelineFactory>>(),
                sp.GetRequiredService<ILoggerFactory>(),
                sp.GetService<IDataGateway>(),
                new Lazy<IPlatformServiceProvider<IGenericConnection, IGenericConfiguration>>(
                    () => sp.GetService<IPlatformServiceProvider<IGenericConnection, IGenericConfiguration>>()!),
                sp.GetService<IDataStoreProvider>()));

            // Why: Lazy<IConfigurationGateway> defers cfg resolution until first runtime query, avoiding
            // circular dependency with the DataGateway that hasn't been built yet at registration time.
            // DataStore flows from TypeCollection.Configure() so "ConfigurationDb" is never hardcoded here.
            builder.Services.AddSingleton(sp => new DefaultConfigurationProvider<BatchCopyPipelineConfiguration, BatchCopyPipelineConfigurationCommand>(
                sp.GetRequiredService<ILoggerFactory>().CreateLogger<DefaultConfigurationProvider<BatchCopyPipelineConfiguration, BatchCopyPipelineConfigurationCommand>>(),
                sp.GetRequiredService<Lazy<IConfigurationGateway>>(),
                DataStore,
                PathName,
                new Lazy<ICacheInvalidator?>(() => sp.GetService<ICacheInvalidator>())));

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
