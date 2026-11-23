using System;
using System.Linq;
using Fdw.Operations.Abstractions.Thresholds;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.AnswerRequests;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Abstractions.Thresholds;
using Fdw.Services.Etl.Execution;
using Fdw.Services.Etl.Execution.Answering;
using Fdw.Services.Etl.Execution.AtOnceRecovery;
using Fdw.Services.Etl.Execution.Claims;
using Fdw.Services.Etl.Flow;
using Fdw.Services.Etl.Thresholds;
using Fdw.Services.Etl.Transforms;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Abstractions.Runs;
using Fdw.Services.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl;

/// <summary>
/// The ETL execution services a pipeline type and an orchestration node register (FDW-6244): the run queue and what runs, records, holds and stops runs,
/// and the execution-inspection services the test-mode endpoints take. Formerly members of <c>EtlPipelineTypes</c>, which this release removes.
/// </summary>
/// <remarks>
/// Why a static registration class and not members of a pipeline type base: the graph pipeline type's Registration and the orchestration node's both call
/// it, and neither is the other. It sits in the package that owns the registered types, as <c>OrchestrationNodeReadRegistration</c> does; it names the ETL
/// execution types, which <c>PipelineTypes</c> cannot see. Every method is idempotent, so the callers may call it in any order.
/// </remarks>
public static class PipelineExecutionRegistration
{
    /// <summary>
    /// Registers the pipeline execution queue (<see cref="IPipelineExecutionQueue"/>), its background consumer
    /// (<see cref="PipelineExecutionBackgroundService"/>) and the services that run, hold, record and stop runs.
    /// </summary>
    /// <remarks>
    /// The orchestrator (Fdw.Services.Etl.Projects) takes IPipelineExecutionQueue as a hard constructor dependency to dispatch leaf-node runs, so every host
    /// that registers a graph pipeline type or the orchestration gets a working queue. The Pipeline domain registers the run contract (<c>IPipelineRuns</c>)
    /// itself; this registers its Queued runner and the run starter over it.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    public static void RegisterPipelineExecutionQueue(IServiceCollection services)
    {
        // The requests of the runs that wait for approval (FDW-1937), kept beside the queue they would have entered.
        services.TryAddSingleton<IPipelineRunHolds, PipelineRunHolds>();

        if (services.Any(d => d.ServiceType == typeof(IPipelineExecutionQueue)))
            return;

        // Why only a host that names its operational store hosts the services that run pipelines (FDW-3975): the ETL host names the store its runs are kept in. A host that only
        // sends runs to it, the scheduler, has no queue to consume, no stored requests to take up again (a second host taking up the ETL host's runs would run them twice) and
        // no alerts to time, and it cannot read the store it does not name, so these services would only log that.
        var hostsRuns = PipelineTypes.OperationalConnection is { Length: > 0 };

        // What the host keeps of each run it accepts, so a restart can take the runs that were held or waiting up again (FDW-3216). The queue takes the
        // request of every run it accepts into it.
        services.TryAddSingleton<IPipelineRunRequestStore, PipelineRunRequestStore>();
        services.TryAddSingleton<IPipelineTriggerKeys, PipelineTriggerKeys>();

        // The thresholds on waiting runs (FDW-3161, FDW-3975): one monitor is the observer the scheduler tells, the check a trigger asks before it takes a run (the gate asks
        // the evaluator which HoldNewRuns rules hold), and the loop that has the evaluator judge the thresholds attached to pipelines, schedules and the server.
        services.TryAddSingleton<IWaitThresholdSource, WaitThresholdSource>();
        services.TryAddScoped<IWaitThresholdStore, WaitThresholdStore>();
        services.TryAddScoped<IPipelineFailureConfigurations, PipelineFailureConfigurations>();
        services.TryAddScoped<IPipelineRunHistory, PipelineRunHistory>();
        services.TryAddScoped<IPipelineDisableRecord, PipelineDisableRecord>();
        services.TryAddScoped<IPipelineDisabler, PipelineDisabler>();
        services.TryAddScoped<PipelineFailureReporter>();
        services.TryAddSingleton<WaitHoldGate>();
        services.TryAddSingleton<WaitThresholdReporter>();
        services.TryAddSingleton<WaitThresholdMonitor>();
        services.TryAddSingleton<IPipelineWaitObserver>(provider => provider.GetRequiredService<WaitThresholdMonitor>());
        services.TryAddSingleton<IPipelineRunStopCheck>(provider => provider.GetRequiredService<WaitThresholdMonitor>());
        if (hostsRuns)
        {
            services.AddHostedService(provider => provider.GetRequiredService<WaitThresholdMonitor>());
        }

        // The number of triggers the queue holds is a setting (FDW-3155), read when the host first needs the queue.
        services.AddSingleton(PipelineExecutionQueue.Create);
        services.TryAddSingleton<PipelineRunScheduler>();

        // What runs one stored request through the engine: the queued executor calls it for a run that has taken its place, and the AtOnce runner for a run that starts at once (FDW-6631).
        services.TryAddSingleton<PipelineRequestProcessor>();
        services.TryAddSingleton<IPipelineRequestProcessor>(provider => provider.GetRequiredService<PipelineRequestProcessor>());

        // What the live view reads: the components of the runs in flight (FDW-3382).
        services.TryAddSingleton<RunningFlows>();

        // What the host holds of each run it has queued, so cancelling, pausing and resuming an execution reaches the run (FDW-2950).
        services.TryAddSingleton<PipelineRunControl>();
        services.TryAddSingleton<Fdw.Operations.Abstractions.Execution.IExecutionRunControl>(
            provider => provider.GetRequiredService<PipelineRunControl>());

        // How the StopRun escalation of a threshold stops a run this host holds (FDW-3865).
        services.TryAddSingleton<Fdw.Operations.Abstractions.Thresholds.IRunStopper>(
            provider => new ThresholdRunStopper(provider.GetRequiredService<PipelineRunControl>()));
        services.AddSingleton<IPipelineExecutionQueue>(provider => provider.GetRequiredService<PipelineExecutionQueue>());

        // How long a stopping host lets the runs in flight finish is a setting (FDW-3786), read when the host first needs it; the host's shutdown timeout
        // follows it, since the default of the generic host (30 seconds) would end the host before the runs have had their time.
        services.AddSingleton(PipelineStopGrace.Create);
        services.AddOptions<Microsoft.Extensions.Hosting.HostOptions>()
            .Configure<PipelineStopGrace>((options, grace) => options.ShutdownTimeout = grace.ShutdownTimeout);
        if (hostsRuns)
        {
            services.AddHostedService<PipelineExecutionBackgroundService>();
            services.AddHostedService(provider => provider.GetRequiredService<PipelineAtOnceRuns>());
            services.AddHostedService<PipelineComponentsPublisher>();
        }

        // The timer that sends the levels of open escalation alerts as they fall due (FDW-3863). Only the host that runs pipelines runs it: the alerts are opened by thresholds
        // judged there, and a host that serves requests only has no use for a timer.
        services.TryAddSingleton(TimeProvider.System);
        if (hostsRuns)
        {
            services.AddHostedService<Fdw.Operations.Escalation.EscalationBackgroundService>();
        }

        // A pipeline that starts another and an orchestration node that dispatches its members both start runs through the starter (FDW-1381).
        // Why a factory over IPipelineRuns (FDW-6131): the starter is the run contract's IPipelineRunStarter face, so a run a run starts is handed to the
        // runner of the started pipeline type's run mode like every other start. The starter's other constructor queues every run and is not the host's.
        // The holder of the limits of the runs carried out at once (FDW-6701) is registered below for every host, so a run an at-once run starts is made at once under what is left of
        // its limits.
        services.TryAddScoped<IPipelineRunStarter>(provider => new PipelineRunStarter(
            provider.GetRequiredService<IPipelineRuns>(),
            provider.GetRequiredService<IPipelineConfigurationProvider>(),
            provider.GetRequiredService<PipelineAtOnceRuns>(),
            provider.GetService<ILogger<PipelineRunStarter>>(),
            provider.GetService<IPipelineOffers>(),
            provider.GetService<IPipelineRunOutputs>()));

        RegisterRunServices(services);

        // What a run's row states about where the run came from (FDW-6519): the host's application name and the slot of the one reader of the slot file.
        services.TryAddSingleton<IPipelineRunOrigins, PipelineRunOrigins>();

        RegisterRunClaims(services);

        // The runner of the Queued run mode (FDW-6131), beside the queue it feeds. Registered by implementation type, so a second registration is a no-op.
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IPipelineRunner, QueuedPipelineRunner>());

        // The runner of the AtOnce run mode (FDW-6141), in this assembly because the Interactive type which runs AtOnce is: a type and the runner of its run mode are never
        // separated by a package boundary, or a host that runs pipelines would be refused at start for a run mode nothing carries out. The host-wide holder of its runs is a
        // hosted service of a host that runs pipelines, so the host's stop reaches the runs and waits for them.
        services.TryAddSingleton<PipelineAtOnceRuns>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IPipelineRunner, AtOnceRunner>());

        // The row-flow engine (FDW-1386). It holds no per-run state, so one executor serves every run.
        services.TryAddSingleton<Fdw.Services.Etl.Abstractions.Flow.IFlowExecutor, Fdw.Services.Etl.Flow.FlowExecutor>();
    }

    // What a run is given by the stories of the on-request pipelines, in a method of their own so the registration of the queue stays within the method length the build allows: the offers
    // (FDW-6135), the stored grants (FDW-6144), the outputs (FDW-6136), who a schedule acts as (FDW-6144), the pieces a run gives its person (FDW-6140) and the claims of steps (FDW-6139).
    private static void RegisterRunServices(IServiceCollection services)
    {
        // The pipelines a tenant and a person are offered (FDW-6135), the pipeline's own configuration read and written through the platform's configuration gateway: the one
        // service answers the starter's reads and the routes that list, add and remove offers.
        services.TryAddScoped<PipelineOffers>();
        services.TryAddScoped<IPipelineOffers>(provider => provider.GetRequiredService<PipelineOffers>());
        services.TryAddScoped<IPipelineOfferService>(provider => provider.GetRequiredService<PipelineOffers>());

        // The stored grants of runs (FDW-6144): what runs may do as a person while they are away, and until when; a schedule and an answer request store a grant's id.
        services.TryAddScoped<IRunGrants, RunGrants>();
        // The answer requests of runs (FDW-6142): a run keeps one for the person it acts as and ends, and the person's answer starts the continuation once. The policy that says which calls need
        // the person, which a stored grant covers and how long a request lasts is configuration, read through the platform's configuration gateway.
        services.TryAddScoped<IAnswerRequests, AnswerRequests>();
        services.TryAddScoped<IAnswerPolicies, AnswerPolicies>();
        // What a run gave back (FDW-6136): the outputs its pipeline declares, written when it ends and read by the pipeline that started it and waited.
        services.TryAddScoped<IPipelineRunOutputs, PipelineRunOutputs>();

        // Who a schedule's fire acts as (FDW-6144): the person the schedule's stored grant names, judged as a run that starts is.
        services.TryAddScoped<IScheduleRunAs, ScheduleRunAs>();
        // The pieces a run gives the person it is for (FDW-6140): kept in order with a sequence a client resumes from, read by that person only (and the audience the host registers).
        services.TryAddScoped<IRunStream, RunStream>();

        // The claims of the steps that act outside the run (FDW-6139): a step is claimed before it acts, so a restart, a retry or a repeated answer does not make it act twice.
        services.TryAddScoped<IPipelineStepClaims, PipelineStepClaims>();
    }

    // The durable claim of runs and the hold on a key, in a method of their own so the registration of the queue stays within the method length the build allows.
    private static void RegisterRunClaims(IServiceCollection services)
    {
        // The durable claim of runs (FDW-6629): the three OpsDb procedures called through the data gateway, with the four settings. A host that runs pipelines judges them when it
        // initializes (FDW-6939, PipelineRunClaimSettings.Gaps), so a setting that is not usable stops the host at start; they are then read once, when first needed, and held. A host that does
        // not run pipelines is not judged and does not claim.
        services.TryAddSingleton(provider =>
        {
            var read = PipelineRunClaimSettings.Read(
                provider.GetRequiredService<IEffectiveSettingsProvider>(), provider.GetRequiredService<ILoggerFactory>().CreateLogger<PipelineRunClaimService>());
            return read.IsSuccess && read.Value is { } settings ? settings : throw new InvalidOperationException(string.Concat(read.CurrentMessage));
        });
        services.TryAddScoped<IPipelineKeyHolds>(provider => new PipelineKeyHoldService(
            provider.GetRequiredService<IDataGatewayProvider>().ByName(DataGatewayNames.Main),
            PipelineTypes.OperationalConnection,
            provider.GetRequiredService<PipelineRunClaimSettings>(),
            provider.GetService<ILogger<PipelineKeyHoldService>>()));
        services.TryAddScoped<IPipelineRunClaimService>(provider => new PipelineRunClaimService(
            provider.GetRequiredService<IDataGatewayProvider>().ByName(DataGatewayNames.Main),
            PipelineTypes.OperationalConnection,
            provider.GetRequiredService<PipelineRunClaimSettings>(),
            provider.GetService<ILogger<PipelineRunClaimService>>(),
            provider.GetService<IPipelineLapsedClaims>()));

        // What the run code claims before a run starts, for both run modes (FDW-6939); scoped, because it reads through the run's own scope.
        services.TryAddScoped<PipelineRunClaiming>();

        // What ends the runs carried out at once whose claim lapsed (FDW-6986): the claim service gives it every lapsed claim a procedure answers, and a host that runs pipelines sweeps for the rest.
        services.TryAddSingleton<IHalfDoneRunRecords, PipelineHalfDoneRunRecords>();
        services.TryAddSingleton<IHalfDonePipelines, PipelineHalfDonePipelines>();
        services.TryAddSingleton<PipelineAtOnceRecovery>();
        services.TryAddSingleton<IPipelineLapsedClaims>(provider => provider.GetRequiredService<PipelineAtOnceRecovery>());
        if (PipelineTypes.OperationalConnection is { Length: > 0 })
        {
            services.AddHostedService<PipelineAtOnceRecoveryService>();
        }
    }

    /// <summary>
    /// Registers the execution-inspection services the test-mode endpoints take (InspectEdgeEndpoint, StepExecutionEndpoint, ResumeTestExecutionEndpoint),
    /// and the calculations a query filter names for its values (FDW-2333).
    /// </summary>
    /// <remarks>
    /// They belong to the ETL execution a graph pipeline runs on, not to one pipeline type's row, and have no implementation of their own.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    public static void RegisterAdditionalServices(IServiceCollection services)
    {
        services.TryAddSingleton<IPipelineExecutionInspector, PipelineExecutionInspector>();
        services.TryAddSingleton<IPipelineTestController, PipelineTestController>();
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IFilterCalculations, BuiltFilterCalculations>();
    }
}
