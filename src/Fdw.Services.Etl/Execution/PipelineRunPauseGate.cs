using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Execution;

/// <summary>The gate one run's flow steps wait at while the run is paused (FDW-2950).</summary>
public sealed class PipelineRunPauseGate : IFlowPauseGate
{
    private readonly Lock _sync = new();
    private TaskCompletionSource<bool>? _held;

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
