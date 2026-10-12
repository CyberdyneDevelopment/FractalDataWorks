using System;
using System.Collections.Generic;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// Who is changing a sign-in flow, and how they signed in.
/// </summary>
/// <remarks>
/// Carried into every change so a restrictive one can be refused when it would leave this administrator
/// no way back in, and so the log names who made it.
/// </remarks>
public sealed record AuthenticationFlowAdministrator
{
    /// <summary>Gets the administrator's user id.</summary>
    public required Guid UserId { get; init; }

    /// <summary>Gets the RFC 8176 methods the administrator's sign-in proved (the token's <c>amr</c>).</summary>
    /// <remarks>
    /// Empty for a caller that did not sign in through a flow, such as an API key: flows do not govern
    /// how that caller gets in, so no change to a flow can lock it out.
    /// </remarks>
    public required IReadOnlyList<string> AuthenticationMethods { get; init; }
}
