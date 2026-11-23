using System;

namespace Fdw.Services.Etl.Execution.Claims;

/// <summary>Whether a run still held its claim when its host renewed it (FDW-6629).</summary>
/// <param name="ExecutionId">The run.</param>
/// <param name="Renewed">True when the lease was renewed; false when the run holds no row of that host and slot any more, so its claim is lost.</param>
public sealed record PipelineRunRenewal(Guid ExecutionId, bool Renewed);
