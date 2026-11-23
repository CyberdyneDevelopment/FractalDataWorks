using Fdw.Services.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Pipelines.Abstractions;
using Fdw.Services.Pipelines.Abstractions.Runs;
using Fdw.Services.Pipelines.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Pipelines.Runs;

/// <summary>
/// The pipeline layer's run contract (FDW-6131): reads the pipeline, takes its pipeline type from the implementation its domain row names, and hands the
/// run to the runner this host registered for that type's run mode.
/// </summary>
/// <remarks>
/// <para>
/// Dispatch is by the source-generated <see cref="PipelineTypes"/> and <see cref="PipelineRunModes"/> lookups and by each runner's own
/// <see cref="IPipelineRunner.RunMode"/>, never by a switch on names: an implementation from another package declares its <see cref="PipelineTypes"/>
/// option (FDW-6244), and the package that hosts its run mode registers the runner.
/// </para>
/// <para>
/// The runners are given by constructor, so nothing is pulled from the container. A run started with its completion kept is remembered with the runner
/// that started it, for <see cref="Await"/>.
/// </para>
/// </remarks>
public sealed class PipelineRuns : IPipelineRuns
{
    private const string LiveGroupPrefix = "execution:";

    private readonly IPipelineConfigurationProvider _pipelines;
    private readonly IReadOnlyList<IPipelineRunner> _runners;
    private readonly IExecutionRunControl _runControl;
    private readonly IExecutionTracker _tracker;
    private readonly IAuthenticationContextAccessor _authentication;
    private readonly ConcurrentDictionary<Guid, IPipelineRunner> _awaitable = new();
    private readonly ILogger<PipelineRuns> _logger;

    /// <summary>Initializes a new instance of the <see cref="PipelineRuns"/> class.</summary>
    /// <param name="pipelines">Reads the pipeline a start names.</param>
    /// <param name="runners">The runners this host carries runs out with, one per run mode.</param>
    /// <param name="runControl">Reaches a run this host holds, to stop it.</param>
    /// <param name="tracker">Reads a run's execution record, to say what state it is in.</param>
    /// <param name="authentication">The caller of a start, whose person the run acts as (FDW-6137).</param>
    /// <param name="logger">The logger.</param>
    public PipelineRuns(
        IPipelineConfigurationProvider pipelines,
        IEnumerable<IPipelineRunner> runners,
        IExecutionRunControl runControl,
        IExecutionTracker tracker,
        IAuthenticationContextAccessor authentication,
        ILogger<PipelineRuns>? logger = null)
    {
        _pipelines = pipelines;
        _runners = [.. runners];
        _runControl = runControl;
        _tracker = tracker;
        _authentication = authentication;
        _logger = logger ?? NullLogger<PipelineRuns>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<Guid>> Start(PipelineStart start, CancellationToken cancellationToken = default)
    {
        var found = await Find(start, cancellationToken).ConfigureAwait(false);
        if (found.IsFailure || found.Value is not { } pipeline)
            return found.ToNewResult<Guid>();

        var runner = RunnerOf(pipeline, start.RequiredRunMode);
        if (runner.IsFailure || runner.Value is not { } chosen)
            return runner.ToNewResult<Guid>();

        var person = PersonOf(start, pipeline);
        if (person.IsFailure)
            return person.ToNewResult<Guid>();

        var required = PersonRequired(pipeline, person.Value);
        if (required.IsFailure)
            return required.ToNewResult<Guid>();

        var started = await chosen.Start(start with { PersonId = person.Value }, pipeline, cancellationToken).ConfigureAwait(false);
        if (started.IsSuccess && start.AwaitCompletion)
            _awaitable[started.Value] = chosen;

        return started;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<bool>> Await(Guid executionId, CancellationToken cancellationToken = default)
    {
        if (!_awaitable.TryRemove(executionId, out var runner))
            return GenericResult<bool>.Failure(PipelineRunsLog.RunNotAwaitable(_logger, executionId));

        return await runner.Await(executionId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public IGenericResult Stop(Guid executionId, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        PipelineRunsLog.RunStopAsked(_logger, executionId, reason);
        return _runControl.Cancel(executionId);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<PipelineRunFollow>> Follow(Guid executionId, CancellationToken cancellationToken = default)
    {
        var read = await _tracker.GetItem(executionId, cancellationToken).ConfigureAwait(false);
        return read.IsFailure || read.Value is not { } item
            ? GenericResult<PipelineRunFollow>.Failure(PipelineRunsLog.RunNotReadable(_logger, executionId, read.CurrentMessage))
            : GenericResult<PipelineRunFollow>.Success(new PipelineRunFollow(
                executionId, LiveGroupPrefix + executionId.ToString("D", CultureInfo.InvariantCulture), item.State.Name));
    }

    // By id or by name, never both and never neither: which of them the caller named is part of what it asked for.
    private async Task<IGenericResult<IPipelineImplementationConfiguration>> Find(PipelineStart start, CancellationToken cancellationToken)
    {
        if (start.PipelineId is { } id)
        {
            return string.IsNullOrWhiteSpace(start.PipelineName)
                ? Found(await _pipelines.Get(id, ConfigurationLevels.WholeAggregate, ConfigurationReadOptions.WithoutFilledMembers, cancellationToken).ConfigureAwait(false), id.ToString("D", CultureInfo.InvariantCulture))
                : GenericResult<IPipelineImplementationConfiguration>.Failure(PipelineRunsLog.PipelineNotNamed(_logger, "both"));
        }

        return string.IsNullOrWhiteSpace(start.PipelineName)
            ? GenericResult<IPipelineImplementationConfiguration>.Failure(PipelineRunsLog.PipelineNotNamed(_logger, "neither"))
            : Found(await _pipelines.Get(start.PipelineName, ConfigurationLevels.WholeAggregate, ConfigurationReadOptions.WithoutFilledMembers, cancellationToken).ConfigureAwait(false), start.PipelineName);
    }

    private IGenericResult<IPipelineImplementationConfiguration> Found(IGenericResult<IPipelineImplementationConfiguration> read, string label) =>
        read.IsFailure || read.Value is null
            ? GenericResult<IPipelineImplementationConfiguration>.Failure(PipelineRunsLog.PipelineNotFound(_logger, label, read.CurrentMessage))
            : read;

    // The person the run acts as (FDW-6137). A start that releases a held run acts as the person who asked for it, kept by the server with the run's request,
    // never as the caller releasing it. A schedule's fire acts as the person its stored grant names, once the server judged the grant (FDW-6144). Any other start acts as its caller, and only its caller: a start that names a person (a start a run makes names its
    // run's) must name its caller, and a caller that is no person (the system, a run acting as no person, a machine client) starts a run that acts as none.
    private IGenericResult<Guid?> PersonOf(PipelineStart start, IPipelineImplementationConfiguration pipeline)
    {
        var released = start.Opened is { ReleasesHeldRun: true };
        var granted = start.Opened is { ActsAsGrantedPerson: true };
        var (person, source) = released
            ? (start.PersonId, "the person who asked for the held run")
            : granted
                ? (start.PersonId, "the person of the schedule's stored grant")
                : (RunPerson.Of(_authentication.Current), "its caller");
        if (!released && !granted && start.PersonId is { } named && named != person)
        {
            return GenericResult<Guid?>.Failure(PipelineRunsLog.RunPersonMismatch(
                _logger, pipeline.Name, named, person is { } acting ? $"{source}, person '{acting}'" : $"{source}, no person"));
        }

        if (person is { } runAs)
            PipelineRunsLog.RunActsAsPerson(_logger, pipeline.Name, runAs, source);

        return GenericResult<Guid?>.Success(person);
    }

    // A type that runs for a person (FDW-6137) is not started by anything that acts as none. A type no option names was refused by the runner's lookup before this is read.
    private IGenericResult PersonRequired(IPipelineImplementationConfiguration pipeline, Guid? person)
    {
        var type = PipelineTypes.ByName(pipeline.Implementation);
        return person is null && !ReferenceEquals(type, PipelineTypes.NotFound) && type.RequiresPerson
            ? GenericResult.Failure(PipelineRunsLog.RunPersonMissing(_logger, pipeline.Name, type.Name))
            : GenericResult.Success();
    }

    // The pipeline type is the PipelineTypes option the domain row's implementation names (FDW-6244); the type names its run mode; the runner is the one
    // registered for that mode.
    // A start that requires a run mode (FDW-6701: the start an at-once run makes) is carried out by the runner of that mode whatever the type says; one that requires none is
    // carried out by the runner of its type's mode.
    private IGenericResult<IPipelineRunner> RunnerOf(IPipelineImplementationConfiguration pipeline, string? requiredRunMode)
    {
        if (requiredRunMode is not null)
            return RunnerOfRequired(pipeline, requiredRunMode);

        var type = PipelineTypes.ByName(pipeline.Implementation);
        if (ReferenceEquals(type, PipelineTypes.NotFound))
            return GenericResult<IPipelineRunner>.Failure(PipelineRunsLog.PipelineTypeUnknown(_logger, pipeline.Name, pipeline.Implementation));

        var mode = PipelineRunModes.ByName(type.RunModeName);
        if (ReferenceEquals(mode, PipelineRunModes.NotFound))
            return GenericResult<IPipelineRunner>.Failure(PipelineRunsLog.PipelineTypeRunModeUnknown(_logger, pipeline.Name, type.Name, type.RunModeName));

        if (_runners.FirstOrDefault(runner => string.Equals(runner.RunMode.Name, mode.Name, StringComparison.Ordinal)) is not { } chosen)
            return GenericResult<IPipelineRunner>.Failure(PipelineRunsLog.PipelineRunModeUnsupported(_logger, pipeline.Name, type.Name, mode.Name));

        PipelineRunsLog.RunHandedToRunner(_logger, pipeline.Name, type.Name, mode.Name);
        return GenericResult<IPipelineRunner>.Success(chosen);
    }

    private IGenericResult<IPipelineRunner> RunnerOfRequired(IPipelineImplementationConfiguration pipeline, string requiredRunMode)
    {
        var mode = PipelineRunModes.ByName(requiredRunMode);
        if (ReferenceEquals(mode, PipelineRunModes.NotFound))
            return GenericResult<IPipelineRunner>.Failure(PipelineRunsLog.RequiredRunModeUnknown(_logger, pipeline.Name, requiredRunMode));

        if (_runners.FirstOrDefault(runner => string.Equals(runner.RunMode.Name, mode.Name, StringComparison.Ordinal)) is not { } chosen)
            return GenericResult<IPipelineRunner>.Failure(PipelineRunsLog.RequiredRunModeUnsupported(_logger, pipeline.Name, mode.Name));

        PipelineRunsLog.RunHandedToRunner(_logger, pipeline.Name, pipeline.Implementation, mode.Name);
        return GenericResult<IPipelineRunner>.Success(chosen);
    }
}
