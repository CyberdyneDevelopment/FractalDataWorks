using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authentication.Abstractions.Context;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// What a step did.
/// </summary>
/// <remarks>
/// A step carries a contribution rather than a context so it cannot rewrite what it did not produce.
/// <see cref="Challenge"/> is what makes an interactive flow work at all: the step says where the
/// caller must go, the runner persists what has been established, and something resumes minutes or
/// hours later. A redirect to an identity provider and a person walking to a terminal are the same
/// mechanism at different timescales.
/// </remarks>
public abstract record StepOutcome
{
    private StepOutcome() { }

    /// <summary>The step produced something.</summary>
    /// <param name="Contribution">What it produced. Filtered to the step's declaration by the runner.</param>
    [ExcludeFromCodeCoverage]
    public sealed record Contributed(ContextContribution Contribution) : StepOutcome;

    /// <summary>The caller must go somewhere and come back.</summary>
    /// <param name="RedirectTo">Where to send them.</param>
    /// <param name="ResumeToken">The single-use token that resumes this execution.</param>
    [ExcludeFromCodeCoverage]
    public sealed record Challenge(Uri RedirectTo, string ResumeToken) : StepOutcome;

    /// <summary>The step is waiting on something out of band.</summary>
    /// <param name="PollAfter">How long to wait before asking again.</param>
    /// <param name="ResumeToken">The single-use token that resumes this execution.</param>
    [ExcludeFromCodeCoverage]
    public sealed record Pending(TimeSpan PollAfter, string ResumeToken) : StepOutcome;

    /// <summary>
    /// This step has nothing to do for this flow, and that is not a failure.
    /// </summary>
    /// <param name="Reason">Why it does not apply.</param>
    /// <remarks>
    /// A principal-resolution step in a client-credentials flow has no external subject to resolve.
    /// It contributes nothing, so any later step requiring that contribution still fails — opting out
    /// is not a way to skip a requirement.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    public sealed record NotApplicable(string Reason) : StepOutcome;
}
