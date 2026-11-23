using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions;
using Fdw.Services.Etl.Abstractions.AnswerRequests;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Execution.Claims;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Graphs.Building;
using Fdw.Services.Etl.Graphs.Logging;
using Fdw.Services.Etl.Thresholds;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.EtlGateway;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Settings;
using Fdw.ServiceTypes;
using Fdw.ServiceTypes.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// The base of a graph pipeline type (FDW-6244): an implementation of the Pipeline domain whose pipelines are graphs (components wired by named input and
/// output nodes) built from a <see cref="GraphPipelineConfiguration"/> over <c>pipe.GraphPipelineConfiguration</c> and run by the row-flow engine; a run
/// takes a <typeparamref name="TIn"/> and answers with a <typeparamref name="TOut"/>.
/// </summary>
/// <typeparam name="TIn">What a run of a pipeline of this type takes.</typeparam>
/// <typeparam name="TOut">What a run of a pipeline of this type answers with.</typeparam>
/// <remarks>
/// <para>
/// It sets no phase. It holds the phase bodies as static methods (<see cref="GraphRegistration"/>, <see cref="GraphInitialization"/>), so every graph
/// pipeline type closing it registers the same things: the graph pipeline factory and
/// provider, the graph configuration provider (registered into the Pipeline domain's provider at Initialize under the option's name), the queue, run
/// control, executor and hosted services (<see cref="PipelineExecutionRegistration.RegisterPipelineExecutionQueue"/>), the inspection services
/// (<see cref="PipelineExecutionRegistration.RegisterAdditionalServices"/>), the readers of a data set's writers, and the domain's provider of ETL
/// pipelines, which each graph pipeline type registers its provider into under its name. A closing type passes its name, description and run mode to the
/// constructor and sets its own phases, each body one call: <c>GraphRegistration(this, builder, loggerFactory)</c> and <c>GraphInitialization(this, host,
/// loggerFactory)</c>. Why the
/// closing sets them and not this base (STC002): only the declared service type sets a phase func, so the option that carries <c>[Implementation]</c> owns
/// its phases and shows at one site what they run.
/// </para>
/// <para>
/// Every closing reads its rows from the one graph row container, <see cref="ContainerName"/>: the row type is fixed by this base, so the container is
/// too, and the graph configuration provider registered once serves every closing under its own name. A container per closing would be read through the
/// first closing's provider.
/// </para>
/// <para>
/// <c>GraphPipelineType</c> closes it over the ETL run (<see cref="PipelineExecutionOptions"/> in, <see cref="IEtlPipelineExecutionResult"/> out), as
/// <see cref="IEtlPipeline"/> closes <see cref="IPipeline{TIn, TOut}"/>.
/// </para>
/// </remarks>
public abstract class GraphPipelineTypeBase<TIn, TOut>
    : PipelineTypeBase<IPipeline<TIn, TOut>, IServiceFactory<IPipeline<TIn, TOut>, GraphPipelineConfiguration>, GraphPipelineConfiguration>
{
    /// <summary>
    /// The key of how often, in seconds, a destination writes the rows it has committed to the run's record (FDW-4957); 0 writes after every
    /// commit. Declared below; a host changes it with <c>ServiceTypeDefaults.Override(new GraphPipelineType(), CommittedReportSeconds, n)</c>
    /// before Register, and the setting <c>PipelineCommittedReportSeconds</c> states it for an installation.
    /// </summary>
    public const string CommittedReportSeconds = "CommittedReportSeconds";

    /// <summary>
    /// The key of how many unmatched keys an update or delete destination's report carries (FDW-4988); 0 carries none, only the count, because keys are values
    /// and may be private. Declared below; a host changes it with <c>ServiceTypeDefaults.Override(new GraphPipelineType(), UnmatchedKeySample, n)</c> before
    /// Register, and the setting <c>PipelineUnmatchedKeySample</c> states it for an installation.
    /// </summary>
    public const string UnmatchedKeySample = "UnmatchedKeySample";

    /// <summary>The container of every graph pipeline type's implementation rows, <c>pipe.GraphPipelineConfiguration</c>.</summary>
    public const string ContainerName = "GraphPipelineConfiguration";

    private IReadOnlyDictionary<string, object>? _declaredDefaults;

    /// <summary>Initializes a new instance of the <see cref="GraphPipelineTypeBase{TIn, TOut}"/> class.</summary>
    /// <param name="name">The implementation's name, as a pipeline's domain row gives it.</param>
    /// <param name="displayName">The display name of this pipeline type.</param>
    /// <param name="description">What a pipeline of this type is.</param>
    /// <param name="runModeName">The run mode a run of this type is carried out in.</param>
    protected GraphPipelineTypeBase(string name, string displayName, string description, string runModeName)
        : base(name, name, displayName, description, runModeName, category: "ETL", defaultContainerName: ContainerName)
    {
        // Every run is judged against thresholds, so a host that runs pipelines without the evaluator is miswired (FDW-3865); the status of a data set is judged by the pipelines that write it
        // and their latest runs, which this option registers a reader of (FDW-4488). A host that never attached this option is asked for neither.
        Requires<IThresholdEvaluator>(RunThresholdLog.EvaluatorNotRegistered);
        Requires<IDataSetWriterRuns>(GraphLog.DataSetWriterRunsNotRegistered);
    }

    /// <summary>
    /// The Initialize body every graph pipeline type sets in its own constructor, as <c>Initialization((host, loggerFactory) => GraphInitialization(this, host, loggerFactory))</c>: the graph configuration provider registered on the Pipeline
    /// domain provider under the option's name, the component types checked against their builders and declared settings, and the build contributions
    /// constructed and checked against every builder's needs.
    /// </summary>
    /// <param name="type">The graph pipeline type being initialized: its name keys the row provider.</param>
    /// <param name="host">The built host.</param>
    /// <param name="loggerFactory">The logger factory, if the host has one.</param>
    /// <returns>The host, or every gap found.</returns>
    protected static IGenericResult<IHost> GraphInitialization(GraphPipelineTypeBase<TIn, TOut> type, IHost host, ILoggerFactory? loggerFactory)
    {
        var services = host.Services;

        // One domain, one implementation: the engine's provider registers under its own
        // name on the Pipeline domain provider. There is no intermediate row to
        // mint or resolve -- pipe.Pipeline already names the implementation.
        services.GetRequiredService<IPipelineConfigurationProvider>()
            .Register(type.Name, services.GetRequiredService<GraphPipelineConfigurationProvider>());

        // Why here, when the host starts (FDW-6133): a component type from another package that ships no builder, or declares a setting with no data type,
        // would otherwise be found when a graph using it is built.
        var logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger(type.GetType());
        List<IGenericMessage> gaps =
        [
            .. GraphFlowBuilder.TypeBuilderMismatches(ComponentTypes.All(), FlowStepBuilders.All(), FlowComponentBuilders.All(), logger),
            .. ComponentSettings.DataTypesNotDeclared(ComponentTypes.All(), logger),
        ];

        // Why the contributions are constructed here (FDW-6132, FDW-6240): a contribution registered twice, or one whose own dependency is missing, would
        // otherwise be found when a graph using its component type is built; each is refused by its own code, and one no builder needs is a warning only.
        // They are scoped, so they are constructed in a scope of their own.
        using var scope = services.CreateScope();
        gaps.AddRange(PipelineRunPerson.JudgeServiceGaps(services, logger));
        gaps.AddRange(PipelineRunClaimSettings.Gaps(services, logger));
        gaps.AddRange(PipelineConfigurationRequirements.Gaps(type, logger));
        var contributions = FlowBuildNeeds.Resolve(scope.ServiceProvider, logger);
        if (contributions.IsFailure || contributions.Value is null)
        {
            return GenericResult<IHost>.Failure([.. gaps, .. contributions.Messages]);
        }

        _ = FlowBuildNeeds.Unused(contributions.Value, FlowStepBuilders.All(), FlowComponentBuilders.All(), logger);
        gaps.AddRange(FlowBuildNeeds.Gaps(FlowStepBuilders.All(), FlowComponentBuilders.All(), contributions.Value, logger));
        return gaps.Count == 0 ? GenericResult<IHost>.Success(host) : GenericResult<IHost>.Failure(gaps);
    }

    /// <summary>
    /// The Register body every graph pipeline type sets in its own constructor, as <c>Registration((builder, loggerFactory) => GraphRegistration(this, builder, loggerFactory))</c>: the declared defaults read, the build contributions registered,
    /// the graph factory and providers, the readers of a data set's writers, the graph configuration provider, the run services
    /// (<see cref="PipelineExecutionRegistration"/>) and the graph pipeline provider registered into the domain's provider of ETL pipelines.
    /// </summary>
    /// <param name="type">The graph pipeline type being registered: its name, declared defaults, path and container.</param>
    /// <param name="builder">The host builder.</param>
    /// <param name="loggerFactory">The logger factory, if the host has one.</param>
    /// <returns>The builder, or the failure that stops registration.</returns>
    protected static IGenericResult<IHostApplicationBuilder> GraphRegistration(GraphPipelineTypeBase<TIn, TOut> type, IHostApplicationBuilder builder, ILoggerFactory? loggerFactory)
    {
        if (!ServiceTypeDefaults.TryResolve(type, CommittedReportSeconds, out var declared) || declared is not int committedReportSeconds || committedReportSeconds < 0)
            return GenericResult<IHostApplicationBuilder>.Failure(GraphLog.CommittedReportSecondsNotDeclared(
                (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger(type.GetType()), type.Name, CommittedReportSeconds));
        if (!ServiceTypeDefaults.TryResolve(type, UnmatchedKeySample, out var declaredSample) || declaredSample is not int unmatchedKeySample || unmatchedKeySample < 0)
            return GenericResult<IHostApplicationBuilder>.Failure(GraphLog.UnmatchedKeySampleNotDeclared(
                (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger(type.GetType()), type.Name, UnmatchedKeySample));

        // Why the graph pipeline type registers the contributions (FDW-6240): a component type's builder is a TypeOption joined by its module initializer, so
        // its package has no Register phase of its own; it names its contributions in Needs, and every one is registered here, once.
        var contributions = FlowBuildNeeds.Register(
            builder.Services, FlowStepBuilders.All(), FlowComponentBuilders.All(), (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger(type.GetType()));
        if (contributions.IsFailure)
        {
            return contributions.ToNewResult<IHostApplicationBuilder>();
        }

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
                sp.GetService<IExecutionTracker>(),
                sp.GetService<RunningFlows>(),
                sp.GetService<IThresholdEvaluator>(),
                sp.GetService<TimeProvider>(),
                sp.GetServices<IFlowBuildContribution>(),
                sp.GetService<IPipelineRunOutputs>(),
                sp.GetService<IPipelineStepClaims>(),
                sp.GetService<IRunStream>(),
                new FlowAskServices(() => sp.GetRequiredService<IAnswerRequests>(), () => sp.GetRequiredService<IAnswerPolicies>(), () => sp.GetRequiredService<IPipelineConfigurationProvider>()))
            {
                CommittedReportSeconds = committedReportSeconds,
                UnmatchedKeySample = unmatchedKeySample,
            },
            ServiceLifetime.Scoped));
        builder.Services.TryAdd(new ServiceDescriptor(
            typeof(IGraphPipelineProvider),
            sp => new GraphPipelineProvider(sp.GetRequiredService<IGraphPipelineFactory>()),
            ServiceLifetime.Scoped));

        // Which pipelines write each data set, read from the pipelines themselves, and the latest run of each, for the status of a data set (FDW-4488).
        builder.Services.TryAddScoped<IDataSetWriters, DataSetWriters>();
        builder.Services.TryAddScoped<IDataSetWriterRuns, DataSetWriterRuns>();

        builder.Services.TryAddSingleton(sp => new GraphPipelineConfigurationProvider(
            sp.GetRequiredService<ILogger<GraphPipelineConfigurationProvider>>(),
            sp.GetRequiredService<IConfigurationGatewayProvider>(),
            PipelineTypes.ConfigurationConnection, type.PathName, type.Container));

        PipelineExecutionRegistration.RegisterPipelineExecutionQueue(builder.Services);
        PipelineExecutionRegistration.RegisterAdditionalServices(builder.Services);
        type.RegisterInPipelineProvider(builder.Services);

        return GenericResult<IHostApplicationBuilder>.Success(builder);
    }

    /// <inheritdoc />
    /// <remarks>
    /// A destination writes what it has committed at most every 10 seconds, besides its first commit, a target emptied and its final figure (FDW-4957); a report
    /// of unmatched keys carries at most 10 of them (FDW-4988).
    /// </remarks>
    public override IReadOnlyDictionary<string, object> DeclaredDefaults
        => _declaredDefaults ??= ServiceTypeDefaults.With(base.DeclaredDefaults, (CommittedReportSeconds, 10), (UnmatchedKeySample, 10));

    // The domain's provider of ETL pipelines: the first graph pipeline type to register adds it over the pipe.Pipeline configuration provider, and every
    // graph pipeline type then registers its own provider into it under its name, wrapping whatever registration is there.
    private void RegisterInPipelineProvider(IServiceCollection services)
    {
        var existing = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IEtlPipelineProvider));
        Func<IServiceProvider, IEtlPipelineProvider> inner = existing?.ImplementationFactory is { } registered
            ? sp => (IEtlPipelineProvider)registered(sp)
            : NewPipelineProvider;
        if (existing is not null)
        {
            services.Remove(existing);
        }

        services.AddScoped<IEtlPipelineProvider>(sp =>
        {
            var provider = inner(sp);
            var factoryResult = provider.Register(Name, () => sp.GetRequiredService<IGraphPipelineProvider>());
            if (!factoryResult.IsSuccess)
            {
                ServiceTypeLog.OptionFactoryRegistrationFailed(
                    sp.GetService<ILoggerFactory>()?.CreateLogger(GetType()) ?? NullLogger.Instance,
                    GetType().Name, Name, nameof(IGraphPipelineFactory), factoryResult.CurrentMessage);
            }

            return provider;
        });
    }

    private static IEtlPipelineProvider NewPipelineProvider(IServiceProvider sp)
    {
        var provider = new EtlPipelineProvider(
            sp.GetService<ILoggerFactory>()?.CreateLogger<EtlPipelineProvider>() ?? NullLogger<EtlPipelineProvider>.Instance);
        var log = sp.GetService<ILoggerFactory>()?.CreateLogger<PipelineTypes>() ?? NullLogger<PipelineTypes>.Instance;
        ServiceTypeLog.DomainProviderConstructing(log, nameof(PipelineTypes), provider.GetType().Name);
        if (sp.GetService<IPipelineConfigurationProvider>() is { } configurationProvider)
        {
            var attached = provider.Register(configurationProvider);
            if (attached.IsSuccess)
                ServiceTypeLog.DomainConfigurationSourceAttached(log, nameof(PipelineTypes), provider.GetType().Name, configurationProvider.GetType().Name);
            else
                ServiceTypeLog.DomainConfigurationSourceRejected(log, nameof(PipelineTypes), provider.GetType().Name, configurationProvider.GetType().Name, attached.CurrentMessage);
        }
        else
        {
            ServiceTypeLog.DomainHasNoConfigurationSource(
                log, nameof(PipelineTypes), provider.GetType().Name, typeof(IImplementationConfigurationProvider<IPipelineImplementationConfiguration>).ToString());
        }

        return provider;
    }
}
