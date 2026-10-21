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
}
