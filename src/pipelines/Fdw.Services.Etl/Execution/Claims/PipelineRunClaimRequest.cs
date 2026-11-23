using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Execution.Claims;

/// <summary>What a host asks when it claims the keys of one pipeline run (FDW-6629).</summary>
/// <param name="TenantId">The tenant the run acts for.</param>
/// <param name="ExecutionId">The run.</param>
/// <param name="PipelineId">The pipeline the run belongs to.</param>
/// <param name="Keys">The keys the run claims; none when it claims nothing.</param>
/// <param name="ExecutingHost">The host's application name, as the run's record states it.</param>
/// <param name="ExecutingSlot">The slot of the host's copy; null for a host that is not on slots.</param>
public sealed record PipelineRunClaimRequest(Guid TenantId, Guid ExecutionId, Guid PipelineId, IReadOnlyList<PipelineRunClaimKey> Keys, string ExecutingHost, string? ExecutingSlot);
