using Fdw.Services.Abstractions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Etl.Abstractions.Execution;
using Fdw.Services.Etl.Execution.Claims;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Pipelines.Loads;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Abstractions.Runs;
using Fdw.Services.Pipelines.Notifications;
using Fdw.Services.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Starts a pipeline run under another execution (FDW-1381): the shared start of an orchestration node's member pipelines and of an Execute pipeline
/// component's target. It is the <see cref="IPipelineRunStarter"/> face of the pipeline layer's run contract (FDW-6131).
/// </summary>
/// <remarks>
/// <para>
/// A host builds it over <see cref="IPipelineRuns"/>, so a start is handed to the runner of the started pipeline's kind like every other start. Built from
/// the queue's own services instead, it starts every run through the Queued run mode's runner (<see cref="QueuedPipelineRunner"/>), which is what a
/// started run was before kinds existed.
/// </para>
/// <para>
/// Why the start refuses instead of choosing: the tenant, the roles, the cause and the pipeline are the caller's to state. A start that leaves one out is
/// not given the surroundings' (no ambient tenant, no empty role list).
/// </para>
/// </remarks>
public sealed class PipelineRunStarter : IPipelineRunStarter
{
    private readonly IPipelineConfigurationProvider _pipelines;
    private readonly IPipelineOffers? _offers;
    private readonly IPipelineRunOutputs? _outputs;
    private readonly Func<PipelineRunStart, CancellationToken, Task<IGenericResult<Guid>>> _start;
    private readonly Func<Guid, CancellationToken, Task<IGenericResult<bool>>> _await;
    private readonly ILogger<PipelineRunStarter> _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineRunStarter"/> class that starts every run through the Queued run mode.</summary>
    /// <remarks>
    /// Why this constructor stays: it is the direct Queued path the existing starter tests build, with no kind routing. Hosts never get it from DI:
    /// <see cref="PipelineExecutionRegistration.RegisterPipelineExecutionQueue"/> registers the starter only through a factory over <see cref="IPipelineRuns"/>, so
    /// every host routes a started run by its kind.
    /// </remarks>
    /// <param name="tracker">Creates the execution item of the run.</param>
    /// <param name="dataGateways">Writes the run row.</param>
    /// <param name="pipelines">Finds the pipeline to start and the parameters it declares.</param>
    /// <param name="queue">Takes the request the executor runs.</param>
    /// <param name="scheduler">Decides how many pipelines run at once; a run that waits on a pipeline it started hands its place back.</param>
    /// <param name="broadcaster">Announces that the run was triggered.</param>
    /// <param name="origins">Makes what the run's row states about where it came from (FDW-6519).</param>
    /// <param name="settings">The settings the length of a chain of runs is judged against (FDW-6135).</param>
    /// <param name="signaler">Keeps a run's completion for a caller that awaits it (optional; a host that orchestrates has one).</param>
    /// <param name="logger">The logger.</param>
    /// <param name="offers">Reads the pipelines a tenant and a person are offered (FDW-6135); null when the host reads none, and a start that needs an offer is then refused.</param>
    /// <param name="outputs">Reads the outputs a run gave back (FDW-6136); null when the host records none, and a step that reads outputs is then refused.</param>
    public PipelineRunStarter(
        IExecutionTracker tracker,
        IDataGatewayProvider dataGateways,
        IPipelineConfigurationProvider pipelines,
        IPipelineExecutionQueue queue,
        PipelineRunScheduler scheduler,
        IPipelineStatusBroadcaster broadcaster,
        IPipelineRunOrigins origins,
        IEffectiveSettingsProvider settings,
        IExecutionCompletionSignaler? signaler = null,
        ILogger<PipelineRunStarter>? logger = null,
        IPipelineOffers? offers = null,
        IPipelineRunOutputs? outputs = null)
    {
        _pipelines = pipelines;
        _offers = offers;
        _outputs = outputs;
        _logger = logger ?? NullLogger<PipelineRunStarter>.Instance;
        var queued = new QueuedPipelineRunner(tracker, dataGateways, queue, scheduler, broadcaster, origins, settings, signaler, (ILogger)_logger);
        _start = async (start, ct) =>
        {
            var found = await Find(start, ct).ConfigureAwait(false);
            return found.IsFailure || found.Value is not { } pipeline
                ? found.ToNewResult<Guid>()
                : await queued.Start(StartOf(start, null), pipeline, ct).ConfigureAwait(false);
        };
        _await = queued.Await;
    }

    /// <summary>Initializes a new instance of the <see cref="PipelineRunStarter"/> class over the pipeline layer's run contract.</summary>
    /// <param name="runs">Hands each start to the runner of the started pipeline's kind.</param>
    /// <param name="pipelines">Finds the pipeline a check names and the parameters it declares.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="offers">Reads the pipelines a tenant and a person are offered (FDW-6135); null when the host reads none, and a start that needs an offer is then refused.</param>
    /// <param name="outputs">Reads the outputs a run gave back (FDW-6136); null when the host records none, and a step that reads outputs is then refused.</param>
    public PipelineRunStarter(
        IPipelineRuns runs, IPipelineConfigurationProvider pipelines, ILogger<PipelineRunStarter>? logger = null, IPipelineOffers? offers = null, IPipelineRunOutputs? outputs = null)
    {
        _pipelines = pipelines;
        _offers = offers;
        _outputs = outputs;
        _logger = logger ?? NullLogger<PipelineRunStarter>.Instance;
        _start = (start, ct) => runs.Start(StartOf(start, null), ct);
        _await = runs.Await;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineRunStarter"/> class over the pipeline layer's run contract, for a host that carries out runs at once (FDW-6701): a start
    /// under an at-once run is made at once, under what is left of that run's limits.
    /// </summary>
    /// <param name="runs">Hands each start to the runner of the started pipeline's kind.</param>
    /// <param name="pipelines">Finds the pipeline a check names and the parameters it declares.</param>
    /// <param name="atOnceRuns">Holds the limits of the at-once runs this host carries out.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="offers">Reads the pipelines a tenant and a person are offered (FDW-6135); null when the host reads none, and a start that needs an offer is then refused.</param>
    /// <param name="outputs">Reads the outputs a run gave back (FDW-6136); null when the host records none, and a step that reads outputs is then refused.</param>
    public PipelineRunStarter(
        IPipelineRuns runs, IPipelineConfigurationProvider pipelines, PipelineAtOnceRuns atOnceRuns, ILogger<PipelineRunStarter>? logger = null, IPipelineOffers? offers = null,
        IPipelineRunOutputs? outputs = null)
    {
        _pipelines = pipelines;
        _offers = offers;
        _outputs = outputs;
        _logger = logger ?? NullLogger<PipelineRunStarter>.Instance;
        _start = (start, ct) => runs.Start(StartOf(start, atOnceRuns), ct);
        _await = runs.Await;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<Guid>> Start(PipelineRunStart start, CancellationToken cancellationToken = default)
    {
        // Why before the run is handed on (FDW-6135): a pipeline a variable named, which a model may have filled, is started only when it is offered to the run's tenant and person.
        if (start.RequiresOffer)
        {
            var offered = await Offered(start, cancellationToken).ConfigureAwait(false);
            if (offered.IsFailure)
                return offered.ToNewResult<Guid>();
        }

        return await _start(start, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IGenericResult> Offered(PipelineRunStart start, CancellationToken ct)
    {
        var label = start.PipelineName is { Length: > 0 } named ? named : start.PipelineId?.ToString("D", CultureInfo.InvariantCulture) ?? string.Empty;
        if (_offers is null)
            return GenericResult.Failure(PipelineRunStartLog.PipelineOffersUnavailable(_logger, label));

        var found = await Find(start, ct).ConfigureAwait(false);
        if (found.IsFailure || found.Value is not { } pipeline)
            return found;

        var offered = await _offers.IsOffered(pipeline.Id, start.PersonId, [.. start.RunRoles], ct).ConfigureAwait(false);
        if (offered.IsFailure)
            return GenericResult.Failure(PipelineRunStartLog.PipelineOffersNotRead(_logger, pipeline.Name, offered.CurrentMessage));

        return offered.Value ? GenericResult.Success() : GenericResult.Failure(PipelineRunStartLog.PipelineNotOffered(_logger, pipeline.Name, start.TenantId));
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Check(string pipelineName, IReadOnlyList<string> parameterNames, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pipelineName))
            return GenericResult.Failure(PipelineRunStartLog.CheckedPipelineNameRequired(_logger));

        var found = await _pipelines.Get(pipelineName, ConfigurationLevels.WholeAggregate, ConfigurationReadOptions.WithoutFilledMembers, cancellationToken).ConfigureAwait(false);
        if (found.IsFailure || found.Value is null)
            return GenericResult.Failure(PipelineRunStartLog.PipelineNotStartable(_logger, pipelineName, NotFoundReason(found)));

        var declared = (found.Value as IDeclaresRunParameters)?.DeclaredRunParameters;
        return PipelineRunParameterValues.VerifyNames(declared, parameterNames, _logger);
    }

    /// <inheritdoc />
    public async Task<bool> Await(Guid executionId, CancellationToken cancellationToken = default)
    {
        // The run that waits hands its durable claim back while it waits, so a pipeline it started whose claim conflicts with its own can run and end (FDW-6939); it claims again before it goes on.
        var handedBack = await PipelineRunClaimHold.YieldCurrent(cancellationToken).ConfigureAwait(false);
        await using (handedBack.ConfigureAwait(false))
        {
            var awaited = await _await(executionId, cancellationToken).ConfigureAwait(false);
            return awaited.IsSuccess && awaited.Value;
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<PipelineOutputValue>>> Outputs(Guid executionId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return _outputs is null
            ? GenericResult<IReadOnlyList<PipelineOutputValue>>.Failure(PipelineOutputLog.NotRead(_logger, executionId, "the host records no outputs of runs"))
            : await _outputs.Read(executionId, tenantId, cancellationToken).ConfigureAwait(false);
    }

    // A start under another execution, as the pipeline layer states it: the run it belongs under is its parent and its correlation. A parent the host holds as an at-once run
    // (FDW-6701) makes the start at once, under what is left of that run's limits; a parent it does not hold changes nothing.
    private static PipelineStart StartOf(PipelineRunStart start, PipelineAtOnceRuns? atOnceRuns)
    {
        var parentLimits = start.ParentExecutionId is { } parent ? atOnceRuns?.LimitsOf(parent) : null;
        return new PipelineStart
        {
            PipelineId = start.PipelineId,
            PipelineName = start.PipelineName,
            TriggerSource = start.TriggerSource,
            TenantId = start.TenantId,
            RunRoles = start.RunRoles,
            Parameters = start.Parameters,
            ParentExecutionId = start.ParentExecutionId,
            AwaitCompletion = start.AwaitCompletion,
            PersonId = start.PersonId,
            RequiredRunMode = parentLimits is null ? null : PipelineRunModes.ByName("AtOnce").Name,
            ParentLimits = parentLimits
        };
    }

    // By id or by name, never both and never neither: which of them the caller named is part of what it asked for.
    private async Task<IGenericResult<IPipelineImplementationConfiguration>> Find(PipelineRunStart start, CancellationToken ct)
    {
        var byId = start.PipelineId is not null;
        var byName = !string.IsNullOrWhiteSpace(start.PipelineName);
        if (byId == byName)
        {
            return GenericResult<IPipelineImplementationConfiguration>.Failure(PipelineRunStartLog.PipelineNotNamed(
                _logger, start.ParentExecutionId, byId ? "both" : "neither"));
        }

        var found = start.PipelineId is { } id
            ? await _pipelines.Get(id, ConfigurationLevels.WholeAggregate, ConfigurationReadOptions.WithoutFilledMembers, ct).ConfigureAwait(false)
            : await _pipelines.Get(start.PipelineName ?? string.Empty, ConfigurationLevels.WholeAggregate, ConfigurationReadOptions.WithoutFilledMembers, ct).ConfigureAwait(false);
        if (found.IsFailure || found.Value is null)
        {
            var label = start.PipelineName is { } named ? named : start.PipelineId?.ToString("D", CultureInfo.InvariantCulture) ?? string.Empty;
            return GenericResult<IPipelineImplementationConfiguration>.Failure(
                PipelineRunStartLog.PipelineNotStartable(_logger, label, NotFoundReason(found)));
        }

        return found;
    }

    // The provider answers a pipeline it cannot find with no message; the refusal says what that means.
    private static string NotFoundReason(IGenericResult found) =>
        found.CurrentMessage is { Length: > 0 } message ? message : "no pipeline of that name is visible to the run";
}
