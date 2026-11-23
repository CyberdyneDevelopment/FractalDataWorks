using System;

namespace Fdw.Services.Etl.Execution.AtOnceRecovery;

/// <summary>What the recovery of a half-done run needs to know of a run's record: its pipeline, the run mode that carried it out, and whether it is still open (FDW-6986).</summary>
/// <param name="Id">The run's execution id.</param>
/// <param name="PipelineName">The pipeline the run is of.</param>
/// <param name="RunMode">The run mode that carried the run out, or null when its row states none.</param>
/// <param name="IsOpen">Whether the run's execution item has not reached a terminal state.</param>
internal sealed record HalfDoneRunRow(Guid Id, string PipelineName, string? RunMode, bool IsOpen);
