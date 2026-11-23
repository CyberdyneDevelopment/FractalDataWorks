using System.Collections.Generic;

namespace Fdw.Services.Etl.Execution.Claims;

/// <summary>What a release answered (FDW-6629).</summary>
/// <param name="Promoted">The waiting runs the release promoted, in the order they were promoted.</param>
/// <param name="Lapsed">The claims of runs the call found lapsed.</param>
public sealed record PipelineRunReleased(IReadOnlyList<PipelinePromotedRun> Promoted, IReadOnlyList<PipelineRunLapsedClaim> Lapsed);
