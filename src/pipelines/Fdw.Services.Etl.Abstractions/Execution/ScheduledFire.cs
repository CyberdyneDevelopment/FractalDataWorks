using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions.Execution;

/// <summary>A schedule's fire as the trigger has it when it asks who the run acts as (FDW-6144): the schedule the fire relays, and the pipeline, tenant, roles and run it is for.</summary>
/// <param name="ScheduleId">The schedule's Id, as the fire relays it.</param>
/// <param name="PipelineId">The pipeline the fire starts.</param>
/// <param name="PipelineName">The pipeline's name, for the refusals to name.</param>
/// <param name="TenantId">The tenant the run belongs to.</param>
/// <param name="ExecutionId">The run's execution item.</param>
/// <param name="RunRoles">The roles the run acts with.</param>
public sealed record ScheduledFire(Guid ScheduleId, Guid PipelineId, string PipelineName, Guid TenantId, Guid ExecutionId, IReadOnlyList<string> RunRoles);
