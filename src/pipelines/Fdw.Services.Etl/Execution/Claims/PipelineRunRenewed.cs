using System.Collections.Generic;

namespace Fdw.Services.Etl.Execution.Claims;

/// <summary>What a renewal answered (FDW-6629).</summary>
/// <param name="Renewals">One answer for each run asked about.</param>
/// <param name="Lapsed">The claims of runs the call found lapsed.</param>
public sealed record PipelineRunRenewed(IReadOnlyList<PipelineRunRenewal> Renewals, IReadOnlyList<PipelineRunLapsedClaim> Lapsed);
