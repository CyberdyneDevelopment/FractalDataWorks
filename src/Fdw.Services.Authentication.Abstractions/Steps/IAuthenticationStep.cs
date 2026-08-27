using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Context;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// One thing that happens on the way to a token.
/// </summary>
/// <remarks>
/// The set of steps is open — a package declares one and a flow names it. What is closed is what a
/// step may contribute, because that vocabulary is what the chain is made of rather than a guess at
/// what anyone might want to do.
/// </remarks>
public interface IAuthenticationStep
{
    /// <summary>Gets what must already be established for this step to run.</summary>
    /// <remarks>
    /// Checked when a flow's configuration loads, so a misordered flow fails at startup with the
    /// missing element named — and again before each execution, because a step that returned
    /// NotApplicable contributed nothing however valid the order was.
    /// </remarks>
    IReadOnlyList<ContextElement> Requires { get; }

    /// <summary>Gets what this step may produce.</summary>
    /// <remarks>
    /// Enforced, not documented. The runner discards anything outside this declaration and reports
    /// it — a declaration nothing checks is a comment.
    /// </remarks>
    IReadOnlyList<ContextElement> Contributes { get; }

    /// <summary>
    /// Gets the authentication method this step proves, as an RFC 8176 value, or null if it proves none.
    /// </summary>
    /// <remarks>
    /// Static metadata the runner records once this step has actually succeeded. A step cannot name
    /// its own method at execution time: one that could would be able to claim a factor it never
    /// checked, and every downstream test of assurance — step-up, high-value operations, audit —
    /// would be unfalsifiable.
    /// </remarks>
    string? AuthenticationMethod { get; }

    /// <summary>Runs the step.</summary>
    /// <param name="context">What the flow has established so far.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IGenericResult<StepOutcome>> Execute(
        AuthenticationContext context, CancellationToken cancellationToken = default);
}
