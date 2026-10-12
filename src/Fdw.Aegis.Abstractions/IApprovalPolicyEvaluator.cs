using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;

namespace Fdw.Aegis.Abstractions;

/// <summary>
/// Deterministically evaluates an <see cref="ApprovalRequest"/> against the declared approval
/// policy and renders a <see cref="Verdict"/>.
/// </summary>
/// <remarks>
/// Asynchronous because the declared commands are read through the AegisCommand domain provider
/// rather than held in memory. Phases 2-4 add human/agent evaluators against this same interface.
/// Fail-closed: an implementation must never throw to signal "deny" — it returns a
/// <see cref="Verdict"/> whose <see cref="Verdict.Disposition"/> is not
/// <see cref="VerdictDispositions.Approve"/>. Failing to read the declared commands is a failure
/// result, never an approval.
/// </remarks>
public interface IApprovalPolicyEvaluator
{
    /// <summary>
    /// Evaluates <paramref name="request"/> and renders a verdict.
    /// </summary>
    /// <param name="request">The submitted approval request.</param>
    /// <param name="cancellationToken">A token to cancel the evaluation.</param>
    /// <returns>
    /// A result carrying the rendered <see cref="Verdict"/>, or a failure (<see cref="AegisResultCodes"/>)
    /// when the request itself is malformed (e.g. missing required parameters) or the declared commands
    /// cannot be read. A well-formed but unapproved request is still a <em>successful</em> evaluation —
    /// its <see cref="Verdict"/> just carries a non-approving disposition.
    /// </returns>
    Task<IGenericResult<Verdict>> Evaluate(ApprovalRequest request, CancellationToken cancellationToken = default);
}
