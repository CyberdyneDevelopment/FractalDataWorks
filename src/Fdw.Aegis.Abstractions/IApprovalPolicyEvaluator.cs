using Fdw.Results;

namespace Fdw.Aegis.Abstractions;

/// <summary>
/// Deterministically evaluates an <see cref="ApprovalRequest"/> against the declared approval
/// policy and renders a <see cref="Verdict"/>.
/// </summary>
/// <remarks>
/// Why synchronous: Phase 1's <c>PreApprovedPolicyEvaluator</c> is a pure lookup against the
/// already-loaded <c>Commands</c>/<c>ApprovalPolicy</c> block — no I/O. Phases 2-4 add human/agent
/// evaluators against this same interface; whichever of those needs to block does so above this
/// seam (e.g. in <c>Aegis.Broker</c>), not by making this interface asynchronous. Fail-closed: an
/// implementation must never throw to signal "deny" — it returns a <see cref="Verdict"/> whose
/// <see cref="Verdict.Disposition"/> is not <see cref="VerdictDispositions.Approve"/>.
/// </remarks>
public interface IApprovalPolicyEvaluator
{
    /// <summary>
    /// Evaluates <paramref name="request"/> and renders a verdict.
    /// </summary>
    /// <param name="request">The submitted approval request.</param>
    /// <returns>
    /// A result carrying the rendered <see cref="Verdict"/>, or a failure (<see cref="AegisResultCodes"/>)
    /// when the request itself is malformed (e.g. missing required parameters). A well-formed but
    /// unapproved request is still a <em>successful</em> evaluation — its <see cref="Verdict"/> just
    /// carries a non-approving disposition.
    /// </returns>
    IGenericResult<Verdict> Evaluate(ApprovalRequest request);
}
