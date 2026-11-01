namespace Fdw.Services.Etl.Execution;

/// <summary>
/// Told by the <see cref="PipelineRunScheduler"/> each time the runs that wait change (FDW-3161): a run joins the list, starts, is withdrawn, or is held or released.
/// </summary>
public interface IPipelineWaitObserver
{
    /// <summary>Says that the runs in flight or waiting changed. It is called after the scheduler has let go of its own lock, so it may read the scheduler.</summary>
    /// <param name="snapshot">The list as it stood after the change.</param>
    void WaitingChanged(PipelineRunQueueSnapshot snapshot);
}
