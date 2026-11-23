using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Execution.Claims;

/// <summary>What a release of a hold answered (FDW-6939).</summary>
/// <param name="HeldBy">The person who had placed the hold that was removed.</param>
/// <param name="Promoted">The waiting runs the release promoted to Claimed, in the order they were promoted.</param>
public sealed record PipelineKeyHoldReleased(Guid HeldBy, IReadOnlyList<PipelinePromotedRun> Promoted);
