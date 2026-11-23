using System;

namespace Fdw.Services.Etl.Execution.Claims;

/// <summary>A waiting run a release promoted to Claimed, so the host that holds it can start it without waiting for its next poll (FDW-6629).</summary>
/// <param name="ExecutionId">The run.</param>
/// <param name="ExecutingHost">The host that holds the run.</param>
/// <param name="ExecutingSlot">The slot of that host's copy, or null.</param>
public sealed record PipelinePromotedRun(Guid ExecutionId, string ExecutingHost, string? ExecutingSlot);
