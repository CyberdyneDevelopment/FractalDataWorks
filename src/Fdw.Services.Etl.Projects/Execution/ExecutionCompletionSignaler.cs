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
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<bool>> _pending =
        new ConcurrentDictionary<Guid, TaskCompletionSource<bool>>();

    private readonly ILogger<ExecutionCompletionSignaler> _logger;

    /// <summary>Initializes a new instance of <see cref="ExecutionCompletionSignaler"/>.</summary>
    public ExecutionCompletionSignaler(ILogger<ExecutionCompletionSignaler>? logger = null)
    {
        _logger = logger ?? NullLogger<ExecutionCompletionSignaler>.Instance;
    }

    /// <inheritdoc/>
    public void Register(Guid executionItemId)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[executionItemId] = tcs;
    }

    /// <inheritdoc/>
    public void Signal(Guid executionItemId, bool succeeded, string? resultMessage)
    {
        ProjectOrchestratorLog.CompletionSignalReceived(_logger, executionItemId);

        if (_pending.TryGetValue(executionItemId, out var tcs))
        {
            tcs.TrySetResult(succeeded);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> Await(Guid executionItemId, CancellationToken cancellationToken = default)
    {
        if (!_pending.TryGetValue(executionItemId, out var tcs))
        {
            ProjectOrchestratorLog.CompletionSignalNotRegistered(_logger, executionItemId);
            return false;
        }

        try
        {
            return await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            ProjectOrchestratorLog.CompletionSignalTimeout(_logger, executionItemId);
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
