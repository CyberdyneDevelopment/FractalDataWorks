using System.Collections.Generic;

namespace Fdw.Services.Etl.Execution.Claims;

/// <summary>
/// Where the lapsed claims every call of the claim procedures answers are given (FDW-6986): a procedure deletes the rows whose lease has passed and answers them to its caller, so a caller that
/// discarded them would lose the runs they name. Nothing is awaited here: the claim call must not wait for the runs to be judged.
/// </summary>
public interface IPipelineLapsedClaims
{
    /// <summary>Takes the claims a procedure answered as lapsed.</summary>
    /// <param name="lapsed">The lapsed claims; empty when none lapsed.</param>
    void Seen(IReadOnlyList<PipelineRunLapsedClaim> lapsed);
}
