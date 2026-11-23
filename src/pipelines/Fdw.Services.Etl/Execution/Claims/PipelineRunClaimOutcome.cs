using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Execution.Claims;

/// <summary>What a claim answered (FDW-6629).</summary>
/// <param name="ExecutionId">The run.</param>
/// <param name="State">Whether the run holds its claim or waits.</param>
/// <param name="Ahead">How many other runs hold, or asked earlier for, the keys it waits on; 0 for a run that is claimed.</param>
/// <param name="Lapsed">The claims of other runs the call found lapsed.</param>
public sealed record PipelineRunClaimOutcome(Guid ExecutionId, IPipelineRunClaimState State, int Ahead, IReadOnlyList<PipelineRunLapsedClaim> Lapsed);
