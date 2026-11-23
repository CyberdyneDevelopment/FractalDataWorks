using System;

namespace Fdw.Services.Etl.Execution.Claims;

/// <summary>A claim whose lease passed without a renewal, removed by a procedure and returned so the engine can resume or end the run (FDW-6629).</summary>
/// <param name="ExecutionId">The run that held it.</param>
/// <param name="TenantId">The run's tenant.</param>
/// <param name="PipelineId">The run's pipeline.</param>
/// <param name="ExecutingHost">The host that held it.</param>
/// <param name="ExecutingSlot">The slot of that host's copy, or null.</param>
/// <param name="State">What the claim was: Claimed or Waiting.</param>
public sealed record PipelineRunLapsedClaim(Guid ExecutionId, Guid TenantId, Guid PipelineId, string ExecutingHost, string? ExecutingSlot, string State);
