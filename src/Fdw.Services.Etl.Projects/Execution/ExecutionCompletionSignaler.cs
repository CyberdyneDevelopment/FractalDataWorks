using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Etl.Projects.Execution;

/// <summary>
/// Singleton TCS registry allowing the project orchestrator to await pipeline completion
/// without polling. Pipeline executions register a completion source; the
/// PipelineExecutionBackgroundService signals them when done.
/// </summary>
public sealed class ExecutionCompletionSignaler : IExecutionCompletionSignaler
{
    // Why ConcurrentDictionary: multiple threads (orchestrator + background service) access
    // the dictionary concurrently — one registering/awaiting, one signaling.
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<bool>> _pending =
        new ConcurrentDictionary<Guid, TaskCompletionSource<bool>>();

    private readonly ILogger<ExecutionCompletionSignaler> _logger;

    /// <summary>Initializes a new instance of <see cref="ExecutionCompletionSignaler"/>.</summary>
    public ExecutionCompletionSignaler(ILogger<ExecutionCompletionSignaler>? logger = null)
    {
        // Why NullLogger fallback: ensures the signaler functions even if DI omits logging.
        _logger = logger ?? NullLogger<ExecutionCompletionSignaler>.Instance;
    }

    /// <inheritdoc/>
    public void Register(Guid executionItemId)
    {
        // Why RunContinuationsAsynchronously: avoids running orchestrator continuations on the
        // signaler's caller thread (which is the pipeline background service's thread).
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[executionItemId] = tcs;
    }

    /// <inheritdoc/>
    public void Signal(Guid executionItemId, bool succeeded, string? resultMessage)
    {
        ProjectOrchestratorLog.CompletionSignalReceived(_logger, executionItemId);

        if (_pending.TryGetValue(executionItemId, out var tcs))
        {
            // Why TrySetResult (not SetResult): the TCS may have been cancelled by the orchestrator
            // (e.g., due to a HaltStage policy). TrySetResult is a no-op on already-completed TCS.
            tcs.TrySetResult(succeeded);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> Await(Guid executionItemId, CancellationToken cancellationToken = default)
    {
        if (!_pending.TryGetValue(executionItemId, out var tcs))
        {
            // Why return false: if we have no TCS registered, the pipeline was never dispatched
            // properly or was already cleaned up — treat as failure.
            ProjectOrchestratorLog.CompletionSignalNotRegistered(_logger, executionItemId);
            return false;
        }

        try
        {
            // Why WaitAsync: allows the cancellation token to interrupt the wait without
            // cancelling the underlying TCS (which would affect the Signal path).
            return await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            ProjectOrchestratorLog.CompletionSignalTimeout(_logger, executionItemId);
            // Why TrySetCanceled with ex.CancellationToken: CA2016 — propagate the original cancellation
            // token from the exception, which is more precise than the method's cancellationToken parameter.
            tcs.TrySetCanceled(ex.CancellationToken);
            return false;
        }
    }

    /// <inheritdoc/>
    public void Deregister(Guid executionItemId)
    {
        _pending.TryRemove(executionItemId, out _);
    }
}
