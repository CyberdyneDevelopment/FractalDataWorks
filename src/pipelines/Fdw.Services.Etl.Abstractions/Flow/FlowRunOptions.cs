namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// How a flow run is carried out (FDW-1386). Nothing here has a default: the caller states the buffering it wants.
/// </summary>
public sealed class FlowRunOptions
{
    /// <summary>
    /// Gets the number of batches a link buffers before a writer waits. One is the tightest back-pressure; a larger number lets a
    /// fast component run ahead of a slow one by that many batches.
    /// </summary>
    public required int LinkCapacity { get; init; }

    /// <summary>
    /// Gets what is told as each component finishes, for whatever counts the rows of the run (FDW-2041); null when nothing counts. This is not a
    /// buffering choice, so it is the one setting here that may be left out.
    /// </summary>
    public IFlowRunProgress? Progress { get; init; }

    /// <summary>
    /// Gets what is told as each stage starts, for whatever records the run by stage (FDW-2060); null when nothing records. Like <see cref="Progress"/>
    /// it is not a buffering choice, so it may be left out.
    /// </summary>
    public IFlowStageObserver? Stages { get; init; }

    /// <summary>Gets the gate every batch passes before it leaves a component, so a paused run holds with the batches it started finished; empty for a run nobody can pause (FDW-2950).</summary>
    public IFlowPauseGate? PauseGate { get; init; }

    /// <summary>Makes the options a run with other progress sees: the same in every other respect, so a field added here cannot be dropped by a copy.</summary>
    /// <param name="progress">The progress the run reports to.</param>
    /// <returns>The options.</returns>
    public FlowRunOptions WithProgress(IFlowRunProgress? progress)
        => new() { LinkCapacity = LinkCapacity, Progress = progress, Stages = Stages, PauseGate = PauseGate };

    /// <summary>Makes the options a run with another stage observer sees: the same in every other respect.</summary>
    /// <param name="stages">The observer of the run's stages.</param>
    /// <returns>The options.</returns>
    public FlowRunOptions WithStages(IFlowStageObserver? stages)
        => new() { LinkCapacity = LinkCapacity, Progress = Progress, Stages = stages, PauseGate = PauseGate };
}
