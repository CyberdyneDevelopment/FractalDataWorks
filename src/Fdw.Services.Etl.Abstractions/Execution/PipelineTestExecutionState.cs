using System.Threading;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>
/// Per-execution mutable state managed by <see cref="IPipelineTestController"/>.
/// </summary>
public sealed class PipelineTestExecutionState
{
    /// <summary>
    /// Gets the <see cref="ManualResetEventSlim"/> used to pause/resume the batch loop.
    /// Initially set (not paused). The pipeline awaits this between batches.
    /// </summary>
    public ManualResetEventSlim PauseEvent { get; } = new ManualResetEventSlim(initialState: true);

    /// <summary>
    /// Gets the <see cref="CancellationTokenSource"/> for this execution.
    /// Abort() calls Cancel() on this source.
    /// </summary>
    public CancellationTokenSource Cts { get; } = new CancellationTokenSource();

    private long _stepPending;

    /// <summary>
    /// Gets or sets whether a step command is pending.
    /// When true, the pipeline re-sets <see cref="PauseEvent"/> after one batch completes.
    /// </summary>
    public bool StepPending
    {
        get => Interlocked.Read(ref _stepPending) == 1L;
        set => Interlocked.Exchange(ref _stepPending, value ? 1L : 0L);
    }
}
