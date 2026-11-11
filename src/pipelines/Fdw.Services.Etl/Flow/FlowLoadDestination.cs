using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Abstractions.Flow;
using Fdw.Services.Etl.Abstractions.OptionTypes;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Pipelines.Loads;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// A destination component that loads each batch it receives through a pipeline's own load mechanism (FDW-1386): a
/// <see cref="LoadUnit"/> run by the <see cref="PipelineLoadExecutor"/>, so insert, upsert and the options of a load mean the
/// same here as they do in a pipeline. It has no output node.
/// </summary>
/// <remarks>
/// <para>
/// Each batch runs in one unit of work. A destination that replaces (load mode Replace) names a truncate unit; it runs once,
/// immediately before the first batch is written, so a source that yields nothing never empties the target.
/// </para>
/// <para>
/// A load that writes a file or files holds its batches back and loads them once, after its input ends (FDW-3193). An absent-key soft delete does not
/// (FDW-4956): each batch's keys are staged in the same unit of work as the batch, and one sweep after the input ends flags the target rows with no
/// staged key, only when every row arrived, a batch was staged, and no row was rejected upstream unless the destination's rule is Proceed. Its stage
/// rows are removed before the first batch and when it ends, however it ends.
/// </para>
/// </remarks>
public sealed class FlowLoadDestination : IFlowComponent, IFlowStageBound, IFlowRejectionsBound
{
    /// <summary>The name of the destination's one input node.</summary>
    public const string InputName = FlowDestination.InputName;

    private readonly string _name;
    private readonly PipelineLoadExecutor _executor;
    private readonly string _pipelineName;
    private readonly LoadUnit _unit;
    private readonly LoadUnit? _truncateFirst;
    private readonly IReadOnlyDictionary<string, object?> _parameters;
    private readonly DateTimeOffset _startedAt;
    private readonly TimeSpan _commitReportInterval;
    private readonly ILogger _logger;
    private readonly bool _skipWrites;
    private FlowStageRecords? _records;
    private int _stage;
    private bool _targetEmptied;

    // An update or delete under the Warn rule (FDW-4988): the keys that matched no row so far, and up to the run's sample size of them.
    private long _unmatched;
    private readonly List<IReadOnlyDictionary<string, object?>> _unmatchedSample = [];

    // A load that stages its keys (FDW-4956): the stage rows it committed, one for each row of each batch, and the reader of the rows rejected upstream.
    private long _staged;
    private Func<long>? _rejectedUpstream;

    // The throttle of what this destination tells the run's records (FDW-4957): when it last wrote, what it last said about the target, and
    // whether a later figure is held back.
    private long? _lastReportedAt;
    private bool _reportedTargetEmptied;
    private bool _reportHeld;

    /// <summary>Initializes a new instance of the <see cref="FlowLoadDestination"/> class.</summary>
    /// <param name="name">The component's name.</param>
    /// <param name="executor">The executor that runs a load unit.</param>
    /// <param name="pipelineName">The pipeline the run belongs to, for the executor's messages.</param>
    /// <param name="unit">The load each batch goes through.</param>
    /// <param name="truncateFirst">A truncate of the same target, run once before the first batch; <see langword="null"/> when the target is not replaced.</param>
    /// <param name="parameters">The run's typed parameter values, for expressions that reference them.</param>
    /// <param name="startedAt">The moment the run began.</param>
    /// <param name="commitReportInterval">
    /// How often the rows committed are written to the run's records (FDW-4957); zero writes after every commit. The first commit, a target emptied
    /// and the final figure are written whatever the interval.
    /// </param>
    /// <param name="logger">The logger.</param>
    /// <param name="skipWrites">Whether this is a test run: the rows are received and counted and none is written.</param>
    public FlowLoadDestination(
        string name,
        PipelineLoadExecutor executor,
        string pipelineName,
        LoadUnit unit,
        LoadUnit? truncateFirst,
        IReadOnlyDictionary<string, object?> parameters,
        DateTimeOffset startedAt,
        TimeSpan commitReportInterval,
        ILogger? logger = null,
        bool skipWrites = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(pipelineName);
        ArgumentNullException.ThrowIfNull(executor);
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentOutOfRangeException.ThrowIfLessThan(commitReportInterval, TimeSpan.Zero);
        _name = name;
        _executor = executor;
        _pipelineName = pipelineName;
        _unit = unit;
        _truncateFirst = truncateFirst;
        _parameters = parameters;
        _startedAt = startedAt;
        _commitReportInterval = commitReportInterval;
        _logger = logger ?? NullLogger.Instance;
        _skipWrites = skipWrites;
    }

    /// <inheritdoc />
    public string Name => _name;

    /// <inheritdoc />
    public IReadOnlyList<string> Inputs => [InputName];

    /// <inheritdoc />
    public IReadOnlyList<string> Outputs => [];

    /// <inheritdoc />
    void IFlowStageBound.BindStages(FlowStageRecords records, int stage)
    {
        _records = records ?? throw new ArgumentNullException(nameof(records));
        _stage = stage;
    }

    /// <inheritdoc />
    void IFlowRejectionsBound.BindRejections(Func<long> rejectedUpstream) => _rejectedUpstream = rejectedUpstream ?? throw new ArgumentNullException(nameof(rejectedUpstream));

    /// <summary>Gets how many rows the load commands affected over the run.</summary>
    public int RowsLoaded { get; private set; }

    /// <summary>
    /// Whether a load must be given every row at once: one that writes a file or files (FDW-3193) writes each file once, so a file is not added to again by a
    /// later batch and a bad value in a late row leaves no file; and an absent-key soft delete that does not stage its keys flags the keys absent from the
    /// rows it is given. One that stages its keys (FDW-4956) and a marker soft delete are written batch by batch.
    /// </summary>
    /// <param name="unit">The load.</param>
    /// <returns><see langword="true"/> when the load must be given every row at once.</returns>
    internal static bool NeedsWholeSet(LoadUnit unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        return unit.Addressing is not null
            || (unit.StageKey is null
                && !string.IsNullOrWhiteSpace(unit.Row.SoftDeleteMode)
                && ReferenceEquals(Fdw.Commands.Data.SoftDeletes.SoftDeleteModes.ByName(unit.Row.SoftDeleteMode), Fdw.Commands.Data.SoftDeletes.SoftDeleteModes.AbsentFromSource));
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Run(IFlowPorts ports, CancellationToken cancellationToken = default)
    {
        var input = ports.Input(InputName);
        if (_skipWrites)
        {
            return await CountOnly(input, cancellationToken).ConfigureAwait(false);
        }

        // A load that stages its keys starts from an empty stage for its key (FDW-4956): a loop's later round never sees an earlier round's keys.
        if (_unit.StageKey is not null)
        {
            var cleared = await _executor.ClearStage(_pipelineName, _unit, cancellationToken).ConfigureAwait(false);
            if (cleared.IsFailure)
            {
                return FlowFailures.Caused(PipelineRunLog.StageNotCleared(_logger, _name, _unit.StageKey, FlowFailures.ReasonOf(cleared)), cleared);
            }
        }

        try
        {
            var loaded = await LoadAll(input, cancellationToken).ConfigureAwait(false);
            return loaded.IsFailure || _unit.StageKey is null ? loaded : await Sweep(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // A figure held back by the interval is written however the destination ends, so the run's records say how far it got (FDW-4957).
            if (_reportHeld)
            {
                await Report(force: true).ConfigureAwait(false);
            }

            // Why not the run's token: the stage rows are removed however the destination ends, a cancel included. A removal that fails is logged and leaves
            // rows that carry the run's execution id; it does not change how the destination ended.
            if (_unit.StageKey is not null)
            {
                var cleared = await _executor.ClearStage(_pipelineName, _unit, CancellationToken.None).ConfigureAwait(false);
                if (cleared.IsFailure)
                {
                    PipelineRunLog.StageNotCleared(_logger, _name, _unit.StageKey, FlowFailures.ReasonOf(cleared));
                }
            }
        }
    }

    // The sweep of a load that staged its keys (FDW-4956), after its input ended. It runs only when every row arrived (a failing component cancels the run's
    // token before its outputs close), when at least one batch was staged (an empty source never flags every row), and when no row was rejected upstream
    // unless the destination's rule is Proceed: a rejected row's key never reached the stage and would look gone.
    private async Task<IGenericResult> Sweep(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            PipelineRunLog.SweepSkippedRunStopped(_logger, _name);
            return GenericResult.Success();
        }

        if (_staged == 0)
        {
            PipelineRunLog.SweepSkippedNothingStaged(_logger, _name);
            return GenericResult.Success();
        }

        if (_rejectedUpstream is not { } rejectedUpstream)
        {
            return GenericResult.Failure(PipelineRunLog.SweepUnjudged(_logger, _name));
        }

        var rule = SweepRejectedRowsRules.ByName(_unit.Row.SweepRejectedRowsRule ?? string.Empty);
        if (ReferenceEquals(rule, SweepRejectedRowsRules.NotFound))
        {
            return GenericResult.Failure(PipelineLoadLog.SweepRejectedRowsRuleUnknown(_logger, _name, _unit.Row.SweepRejectedRowsRule ?? string.Empty));
        }

        var rejected = rejectedUpstream();
        if (rejected > 0 && !rule.SweepsAfterRejections)
        {
            return GenericResult.Failure(PipelineRunLog.SweepRefusedRejectedRows(_logger, _name, rejected));
        }

        var swept = await _executor.Sweep(_pipelineName, _unit, _staged, cancellationToken).ConfigureAwait(false);
        if (swept.IsFailure)
        {
            return FlowFailures.Caused(FlowLog.DestinationLoadFailed(_logger, _name, 0, FlowFailures.ReasonOf(swept)), swept);
        }

        RowsLoaded += swept.Value;
        PipelineRunLog.KeysSwept(_logger, _name, _staged, swept.Value);
        await Report().ConfigureAwait(false);
        FlowLog.DestinationLoaded(_logger, _name, RowsLoaded);
        return GenericResult.Success();
    }

    private async Task<IGenericResult> LoadAll(IFlowInput input, CancellationToken cancellationToken)
    {
        var wholeSet = NeedsWholeSet(_unit);
        var held = new List<IDictionary<string, object?>>();
        var first = true;
        while (await input.Read(cancellationToken).ConfigureAwait(false) is { } batch)
        {
            if (batch.Count == 0)
            {
                continue;
            }

            if (wholeSet)
            {
                held.AddRange(batch.Rows);
                continue;
            }

            var loaded = await Load(batch.Rows, first, cancellationToken).ConfigureAwait(false);
            if (loaded.IsFailure)
            {
                return loaded;
            }

            first = false;
        }

        if (held.Count > 0)
        {
            var loaded = await Load(held, true, cancellationToken).ConfigureAwait(false);
            if (loaded.IsFailure)
            {
                return loaded;
            }
        }

        FlowLog.DestinationLoaded(_logger, _name, RowsLoaded);
        return GenericResult.Success();
    }

    // A destination that writes files empties each file it writes in the same unit of work as it writes it (FDW-3193): the files are known only from the
    // rows, so the truncate cannot run before them, and a file that fails to write is not left emptied.
    private async Task<IGenericResult> LoadFiles(IReadOnlyList<IDictionary<string, object?>> rows, bool first, CancellationToken cancellationToken)
    {
        IReadOnlyList<LoadUnit> units = first && _truncateFirst is not null ? [_truncateFirst, _unit] : [_unit];
        var loaded = await _executor.Run(_pipelineName, units, rows, _parameters, _startedAt, cancellationToken).ConfigureAwait(false);
        if (loaded.IsFailure)
        {
            return FlowFailures.Caused(FlowLog.DestinationLoadFailed(_logger, _name, rows.Count, FlowFailures.ReasonOf(loaded)), loaded);
        }

        RowsLoaded += loaded.Value;
        _targetEmptied |= first && _truncateFirst is not null;
        await Report().ConfigureAwait(false);
        return GenericResult.Success();
    }

    // Tells the run's records what has been committed so far (FDW-3785), so a run that is stopped or lost still says how many rows reached the target and whether it was
    // emptied. Why not the run's token: the rows are committed already, and a cancel must not lose the figure that says so. A run that is not tracked reports nothing.
    // Only the latest total matters (FDW-4957): the first commit and a change in whether the target was emptied are written at once, which is what the retry
    // check reads; after that the row count is written at most once per interval, and a held figure is written when the destination ends.
    private Task Report(bool force = false)
    {
        if (_records is null)
        {
            return Task.CompletedTask;
        }

        var due = force
            || _lastReportedAt is not { } last
            || _targetEmptied != _reportedTargetEmptied
            || Stopwatch.GetElapsedTime(last) >= _commitReportInterval;
        if (!due)
        {
            _reportHeld = true;
            return Task.CompletedTask;
        }

        _reportHeld = false;
        _lastReportedAt = Stopwatch.GetTimestamp();
        _reportedTargetEmptied = _targetEmptied;
        return _records.Committed(
            _stage,
            new DestinationCommit(_name, RowsLoaded, _targetEmptied, NeedsWholeSet(_unit), _truncateFirst is not null || _unit.Kind.RepeatsToSameResult)
            {
                UnmatchedKeys = _unmatched,
                UnmatchedSample = _unmatched == 0 ? null : [.. _unmatchedSample],
            },
            CancellationToken.None);
    }

    // Keys that matched no row under the Warn rule (FDW-4988): the batch committed, so they are counted for the run's records and logged; the Fail rule never
    // reaches here, because its unit rolled back. The sample keeps no more keys than the run's sample size.
    private void CountUnmatched(LoadOutcome outcome)
    {
        // Only a load that reports its unmatched keys answers any, and such a load always states its sample size.
        if (outcome.UnmatchedKeys == 0 || _unit.UnmatchedKeySample is not { } size)
        {
            return;
        }

        _unmatched += outcome.UnmatchedKeys;
        var room = size - _unmatchedSample.Count;
        _unmatchedSample.AddRange(outcome.UnmatchedSample.Take(Math.Max(0, room)));
        PipelineRunLog.UnmatchedKeysWarned(_logger, _name, outcome.UnmatchedKeys, _unmatched, UnmatchedKeyText.Of(outcome.UnmatchedSample));
    }

    // A test run: the rows are received and counted by the link, and none is written.
    private async Task<IGenericResult> CountOnly(IFlowInput input, CancellationToken cancellationToken)
    {
        var received = 0;
        while (await input.Read(cancellationToken).ConfigureAwait(false) is { } batch)
        {
            received += batch.Count;
        }

        FlowLog.DestinationSkippedWrites(_logger, _name, received);
        return GenericResult.Success();
    }

    // The truncate, when one is stated, runs once before the first rows are written, so a source that yields nothing never empties the
    // target.
    private async Task<IGenericResult> Load(IReadOnlyList<IDictionary<string, object?>> rows, bool first, CancellationToken cancellationToken)
    {
        if (_unit.Addressing is not null)
        {
            return await LoadFiles(rows, first, cancellationToken).ConfigureAwait(false);
        }

        if (first && _truncateFirst is not null)
        {
            var emptied = await _executor
                .Run(_pipelineName, [_truncateFirst], [], _parameters, _startedAt, cancellationToken)
                .ConfigureAwait(false);
            if (emptied.IsFailure)
            {
                return FlowFailures.Caused(FlowLog.DestinationTruncateFailed(_logger, _name, FlowFailures.ReasonOf(emptied)), emptied);
            }

            _targetEmptied = true;
            await Report().ConfigureAwait(false);
        }

        var loaded = await _executor
            .RunReporting(_pipelineName, [_unit], rows, _parameters, _startedAt, cancellationToken)
            .ConfigureAwait(false);
        if (loaded.IsFailure || loaded.Value is not { } outcome)
        {
            return FlowFailures.Caused(FlowLog.DestinationLoadFailed(_logger, _name, rows.Count, FlowFailures.ReasonOf(loaded)), loaded);
        }

        CountUnmatched(outcome);

        // The batch's keys were staged in the same unit of work, one stage row for each row (FDW-4956); the executor counts only the rows loaded.
        _staged += _unit.StageKey is null ? 0 : rows.Count;
        RowsLoaded += outcome.RowsLoaded;
        await Report().ConfigureAwait(false);
        return GenericResult.Success();
    }
}
