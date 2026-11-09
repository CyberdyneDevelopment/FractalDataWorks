using System;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// How one run of a pipeline ended, as the failure rule counts it (FDW-3864).
/// </summary>
/// <param name="ExecutionId">The run's execution record.</param>
/// <param name="At">When the run ended.</param>
/// <param name="Succeeded">Whether the run succeeded; a success ends the count.</param>
/// <param name="Counts">
/// Whether a failed run counts toward the threshold: true for a failure other than a transient one (a dependency that could not be reached) and for one with no coded
/// category; false for a transient failure, a run a person cancelled and a run a host stop interrupted. Meaningless for a run that succeeded.
/// </param>
/// <param name="Category">The failure's category name, when it has one.</param>
/// <param name="Code">The failure's code, when it has one.</param>
/// <param name="ConnectionId">The connection that raised the failure (FDW-4192, FDW-4279), when the run recorded one.</param>
/// <param name="TimedOut">
/// Whether the run ended because it passed its time limit (FDW-4278). A timed-out run is never a success and never <paramref name="Counts"/> by itself: whether it counts is
/// the threshold's to say (<see cref="PipelineRunThresholdType.CountedOutcomes"/>), so <see cref="PipelineFailureCounter"/> is asked.
/// </param>
public sealed record PipelineRunOutcome(
    Guid ExecutionId, DateTimeOffset At, bool Succeeded, bool Counts, string? Category, string? Code, Guid? ConnectionId, bool TimedOut = false);
