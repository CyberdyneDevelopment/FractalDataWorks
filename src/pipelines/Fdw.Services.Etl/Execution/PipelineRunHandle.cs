using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// What the host holds of one pipeline run from the moment it is queued until it ends: the token that stops it and the gate that pauses
/// it (FDW-2950). Disposing it removes the run from the control.
/// </summary>
public sealed class PipelineRunHandle : IDisposable
{
    private readonly CancellationTokenSource _cancellation;
    private readonly ConcurrentDictionary<Guid, PipelineRunHandle> _runs;
    private int _cancelRequested;

    internal PipelineRunHandle(Guid executionId, ConcurrentDictionary<Guid, PipelineRunHandle> runs, CancellationToken stoppingToken)
    {
        ExecutionId = executionId;
        _cancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        _runs = runs;
        Gate = new PipelineRunPauseGate();
    }

    /// <summary>Gets the execution row's id.</summary>
    public Guid ExecutionId { get; }

    /// <summary>Gets the token that ends with the host's shutdown or with a cancel of this run.</summary>
    public CancellationToken Token => _cancellation.Token;

    /// <summary>Gets the gate the run's flow steps wait at while it is paused.</summary>
    public PipelineRunPauseGate Gate { get; }

    /// <summary>Gets whether a person cancelled this run, as distinct from the host shutting down.</summary>
    public bool IsCancelled => Volatile.Read(ref _cancelRequested) == 1;

    internal void Cancel()
    {
        Volatile.Write(ref _cancelRequested, 1);
        _cancellation.Cancel();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _runs.TryRemove(new KeyValuePair<Guid, PipelineRunHandle>(ExecutionId, this));
        _cancellation.Dispose();
    }
}
