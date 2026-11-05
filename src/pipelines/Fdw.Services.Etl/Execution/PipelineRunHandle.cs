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
    private int _timedOut;
    private string? _thresholdReason;
    private int _disposed;

    internal PipelineRunHandle(Guid executionId, ConcurrentDictionary<Guid, PipelineRunHandle> runs, CancellationToken stoppingToken)
    {
        ExecutionId = executionId;
        _cancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        _runs = runs;
        Gate = new PipelineRunPauseGate(() => stoppingToken.IsCancellationRequested && !IsCancelled);
    }

    /// <summary>Gets the execution row's id.</summary>
    public Guid ExecutionId { get; }

    /// <summary>Gets the token that ends with the host's shutdown or with a cancel of this run.</summary>
    public CancellationToken Token => _cancellation.Token;

    /// <summary>Gets the gate the run's flow steps wait at while it is paused.</summary>
    public PipelineRunPauseGate Gate { get; }

    /// <summary>Gets whether a person cancelled this run, as distinct from the host shutting down.</summary>
    public bool IsCancelled => Volatile.Read(ref _cancelRequested) == 1;

    /// <summary>Gets whether the run was stopped because its time limit passed (FDW-3805); it then also reads as cancelled.</summary>
    public bool IsTimedOut => Volatile.Read(ref _timedOut) == 1;

    /// <summary>Stops the run because its time limit passed (FDW-3805): it ends the way a cancel ends it, and says it timed out.</summary>
    /// <summary>Gets a value indicating whether a threshold stopped the run (FDW-3865).</summary>
    public bool IsThresholdStopped => Volatile.Read(ref _thresholdReason) is not null;

    /// <summary>Gets why a threshold stopped the run, or <see langword="null"/> when none did.</summary>
    public string? ThresholdReason => Volatile.Read(ref _thresholdReason);

    /// <summary>
    /// Stops the run because a threshold fired and records why (FDW-3865). Only the first stop counts: a run a person already cancelled, that timed out, or that a
    /// threshold already stopped keeps its reason.
    /// </summary>
    /// <param name="reason">Why the threshold stopped the run.</param>
    /// <returns><see langword="true"/> when this stop is the one that ended the run.</returns>
    internal bool StopForThreshold(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (IsCancelled || Volatile.Read(ref _disposed) == 1)
        {
            return false;
        }

        if (Interlocked.CompareExchange(ref _thresholdReason, reason, null) is not null)
        {
            return false;
        }

        Cancel();
        return true;
    }

    internal void TimeOut()
    {
        Volatile.Write(ref _timedOut, 1);
        Cancel();
    }

    internal void Cancel()
    {
        // Why nothing after disposal: a time limit that fires as the run ends finds the token source disposed, and the run it would stop is over.
        if (Volatile.Read(ref _disposed) == 1)
        {
            return;
        }

        Volatile.Write(ref _cancelRequested, 1);
        _cancellation.Cancel();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Volatile.Write(ref _disposed, 1);
        _runs.TryRemove(new KeyValuePair<Guid, PipelineRunHandle>(ExecutionId, this));
        _cancellation.Dispose();
    }
}
