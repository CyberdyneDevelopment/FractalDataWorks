using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Execution;

/// <summary>The gate one run's flow steps wait at while the run is paused (FDW-2950).</summary>
public sealed class PipelineRunPauseGate : IFlowPauseGate
{
    private readonly Lock _sync = new();
    private readonly Func<bool> _stoppedWithHost;
    private TaskCompletionSource<bool>? _held;

    /// <summary>Initializes a new instance of the <see cref="PipelineRunPauseGate"/> class for a run that nothing stops with the host.</summary>
    public PipelineRunPauseGate()
        : this(() => false)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="PipelineRunPauseGate"/> class.</summary>
    /// <param name="stoppedWithHost">Says whether the run was stopped because the host is stopping (FDW-3880).</param>
    internal PipelineRunPauseGate(Func<bool> stoppedWithHost)
    {
        _stoppedWithHost = stoppedWithHost ?? throw new ArgumentNullException(nameof(stoppedWithHost));
    }

    /// <inheritdoc />
    public bool StoppedWithHost => _stoppedWithHost();

    /// <summary>Holds the run before its next batch of rows.</summary>
    /// <returns>False when the run was already paused.</returns>
    public bool Pause()
    {
        lock (_sync)
        {
            if (_held is not null)
            {
                return false;
            }

            _held = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            return true;
        }
    }

    /// <summary>Releases the steps waiting at the gate.</summary>
    /// <returns>False when the run was not paused.</returns>
    public bool Resume()
    {
        TaskCompletionSource<bool>? released;
        lock (_sync)
        {
            released = _held;
            _held = null;
        }

        return released is not null && released.TrySetResult(true);
    }

    /// <inheritdoc />
    public Task WaitWhilePaused(CancellationToken cancellationToken)
    {
        TaskCompletionSource<bool>? held;
        lock (_sync)
        {
            held = _held;
        }

        return held is null ? Task.CompletedTask : held.Task.WaitAsync(cancellationToken);
    }
}
