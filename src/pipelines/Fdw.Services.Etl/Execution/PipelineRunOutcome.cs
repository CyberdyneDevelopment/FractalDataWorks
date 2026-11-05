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
public sealed record PipelineRunOutcome(Guid ExecutionId, DateTimeOffset At, bool Succeeded, bool Counts, string? Category, string? Code);
