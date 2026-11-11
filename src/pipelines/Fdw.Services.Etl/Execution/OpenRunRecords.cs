using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Execution;
using Fdw.Operations.Abstractions.TypeCollections.ExecutionStateOptions;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Closes every record still open under a run the host stopped or lost, and then the run's own record, in one stated terminal state
/// (FDW-3787). A run's stage records and the action records under them are children of its record; a stopped run used to leave them
/// Running for good, because only the run's own record was closed.
/// </summary>
/// <remarks>
/// Why children first: a parent that ends before its children reads as finished while parts of it are still marked running. Why a record
/// that is already terminal is left as it is: a stage that finished, failed or was cancelled before the stop says what happened to it, and
/// closing it again would erase that. Why one routine for the run and everything under it: a cancel (a person's), a graceful stop and the
/// start-up recovery each need exactly this, with their own state, and three copies would drift.
/// </remarks>
internal sealed class OpenRunRecords
{
    private readonly IExecutionTracker _tracker;
    private readonly ILogger _logger;

    public OpenRunRecords(IExecutionTracker tracker, ILogger? logger = null)
    {
        _tracker = tracker;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Closes the open records under a run, then the run.</summary>
    /// <param name="runId">The run's execution id.</param>
    /// <param name="finalState">The terminal state every open record ends in.</param>
    /// <param name="resultCode">The result code each closed record states.</param>
    /// <param name="message">The reason each closed record states.</param>
    /// <param name="cancellationToken">Cancels the closing.</param>
    /// <returns>Success, or the first record that could not be closed (the others are still tried).</returns>
    public async Task<IGenericResult> Close(
        Guid runId, IExecutionStateType finalState, string resultCode, string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resultCode))
            return GenericResult.Failure(PipelineRunRecoveryLog.CloseResultCodeRequired(_logger, runId));

        if (string.IsNullOrWhiteSpace(message))
            return GenericResult.Failure(PipelineRunRecoveryLog.CloseMessageRequired(_logger, runId));

        var closed = new Counter();
        var failure = await CloseUnder(runId, finalState, resultCode, message, closed, cancellationToken).ConfigureAwait(false);
        var run = await _tracker.GetItem(runId, cancellationToken).ConfigureAwait(false);
        if (run.IsFailure || run.Value is null)
        {
            return run;
        }

        var self = await CloseIfOpen(run.Value, finalState, resultCode, message, closed, cancellationToken).ConfigureAwait(false);
        PipelineRunRecoveryLog.OpenRecordsClosed(_logger, runId, closed.Value, finalState.Name);
        return failure.IsFailure ? failure : self;
    }

    private async Task<IGenericResult> CloseUnder(
        Guid parentId, IExecutionStateType finalState, string resultCode, string message, Counter closed, CancellationToken cancellationToken)
    {
        var children = await _tracker.GetChildren(parentId, cancellationToken).ConfigureAwait(false);
        if (children.IsFailure || children.Value is null)
        {
            return children;
        }

        IGenericResult outcome = GenericResult.Success();
        foreach (var child in children.Value)
        {
            var below = await CloseUnder(child.Id, finalState, resultCode, message, closed, cancellationToken).ConfigureAwait(false);
            var self = await CloseIfOpen(child, finalState, resultCode, message, closed, cancellationToken).ConfigureAwait(false);
            outcome = outcome.IsFailure ? outcome : below.IsFailure ? below : self;
        }

        return outcome;
    }

    private async Task<IGenericResult> CloseIfOpen(
        IExecutionItem item, IExecutionStateType finalState, string resultCode, string message, Counter closed, CancellationToken cancellationToken)
    {
        if (item.State.IsTerminal)
        {
            return GenericResult.Success();
        }

        var result = await _tracker.Close(item.Id, finalState, resultCode, message, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            PipelineRunRecoveryLog.RecordNotClosed(_logger, item.Id, result.CurrentMessage);
            return result;
        }

        closed.Value++;
        return result;
    }

    private sealed class Counter
    {
        public int Value { get; set; }
    }
}
