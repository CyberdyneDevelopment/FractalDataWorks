using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.TokenManagers.Abstractions;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// What running a flow produced.
/// </summary>
/// <remarks>
/// The four outcomes are distinct because callers act differently on each. Collapsing them into a
/// boolean is what makes step-up and just-in-time provisioning impossible to implement later.
/// </remarks>
public abstract record FlowResult
{
    private FlowResult() { }

    /// <summary>The flow completed and a token was issued.</summary>
    /// <param name="Token">The issued token.</param>
    [ExcludeFromCodeCoverage]
    public sealed record Completed(IssuedToken Token) : FlowResult;

    /// <summary>The caller must go somewhere and return.</summary>
    /// <param name="RedirectTo">Where to send them.</param>
    /// <param name="ResumeToken">The single-use token that resumes this execution.</param>
    [ExcludeFromCodeCoverage]
    public sealed record Suspended(Uri RedirectTo, string ResumeToken) : FlowResult;

    /// <summary>The flow is waiting on something out of band.</summary>
    /// <param name="PollAfter">How long to wait before asking again.</param>
    /// <param name="ResumeToken">The single-use token that resumes this execution.</param>
    [ExcludeFromCodeCoverage]
    public sealed record Waiting(TimeSpan PollAfter, string ResumeToken) : FlowResult;
}
