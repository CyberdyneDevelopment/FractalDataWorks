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
    /// Gets the RFC 8176 methods this step may assert, or empty if it proves nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A ceiling, not a claim. The runner records the intersection of this set with whatever the
    /// step reports having observed, so a step can report fewer methods than it declares but never
    /// more. What it declares is configuration, which is reviewable; what it reports is discovered
    /// at execution time, which is not.
    /// </para>
    /// <para>
    /// This is why a federating step can pass through the provider's own <c>amr</c> without being
    /// able to inflate it. A provider is the only authority on how someone proved themselves to that
    /// provider — but it is not an authority on what this platform is willing to count.
    /// </para>
    /// </remarks>
    IReadOnlyList<string> AuthenticationMethods { get; }

    /// <summary>Runs the step.</summary>
    /// <param name="context">What the flow has established so far.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IGenericResult<StepOutcome>> Execute(
        AuthenticationContext context, CancellationToken cancellationToken = default);
}
