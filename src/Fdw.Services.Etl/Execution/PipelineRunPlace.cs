using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// A run's place in the scheduler: waiting for its turn, then holding it until the run ends (FDW-3085).
/// </summary>
public sealed class PipelineRunPlace
{
    internal PipelineRunPlace(PipelineRunClaim claim)
    {
        Claim = claim;
    }

    /// <summary>Gets what the run needed to get this place.</summary>
    public PipelineRunClaim Claim { get; }

    /// <summary>Gets when the scheduler accepted the run (FDW-3097). It is kept when the run hands its place back and takes it again.</summary>
    public DateTimeOffset QueuedAt { get; internal set; }

    /// <summary>Gets when the run first started, or <see langword="null"/> for a run that has not (FDW-3097).</summary>
    public DateTimeOffset? StartedAt { get; internal set; }

    /// <summary>Gets a value indicating whether the run was refused: its pipeline already had as many runs as it may have at once, and it refuses a further one (FDW-3117). A refused place never starts.</summary>
    public bool IsRefused => RefusedLimit > 0;

    /// <summary>Gets the limit that refused the run, or zero when the run was not refused.</summary>
    public int RefusedLimit { get; internal set; }

    /// <summary>Gets when a held run joins the queue (FDW-3129), or <see langword="null"/> for a run that is not held.</summary>
    public DateTimeOffset? HeldUntil { get; internal set; }

    // Fires when a held run is due; null for a run that is not held.
    internal ITimer? HoldTimer { get; set; }

    // Replaced each time the run queues, so a run that hands its place back and waits for it again has a task of its own to wait on.
    internal TaskCompletionSource Started { get; set; } = NewStarted();

    // The wait is for this place's own turn. It is answered by a call (WaitAsync), not by handing out the stored task, so a caller never awaits
    // or returns a task that some other part of the code completes.
    internal Task WaitForTurn() => Started.Task.WaitAsync(CancellationToken.None);

    internal static TaskCompletionSource NewStarted() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
