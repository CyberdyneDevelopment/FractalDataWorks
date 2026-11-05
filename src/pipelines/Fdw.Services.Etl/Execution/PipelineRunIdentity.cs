using System;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// What a person needs to recognise a pipeline run in the scheduler's list (FDW-3097). The scheduler's rules do not read it.
/// </summary>
/// <param name="ExecutionId">The execution record of the run.</param>
/// <param name="PipelineId">The pipeline the run is of.</param>
/// <param name="PipelineName">The pipeline's name when the run was accepted.</param>
/// <param name="TenantId">The tenant the run belongs to, or <see langword="null"/> for a run that belongs to none (runs with no tenant count together).</param>
/// <param name="ScheduleId">The Id of the schedule that started the run, or <see langword="null"/> for a run no schedule started (FDW-3975); the thresholds of a schedule are found by it.</param>
/// <param name="ScheduleName">The name of the schedule that started the run, or <see langword="null"/> for a run no schedule started (FDW-3975: the thresholds of a schedule count its waiting runs).</param>
public sealed record PipelineRunIdentity(Guid ExecutionId, Guid PipelineId, string PipelineName, Guid? TenantId, string? ScheduleName = null, Guid? ScheduleId = null);
