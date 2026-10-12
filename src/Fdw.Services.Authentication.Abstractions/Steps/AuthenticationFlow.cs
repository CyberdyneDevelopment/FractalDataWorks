using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Services.Authentication.Abstractions.Steps;

/// <summary>
/// A named, ordered composition of steps.
/// </summary>
/// <remarks>
/// The order is data, not code — which is what stops every login being bespoke. It is still an
/// order rather than a graph: no branches, no loops. A step that needs to decide something decides
/// it inside itself, in code that can be tested, rather than in configuration that cannot.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed record AuthenticationFlow
{
    /// <summary>Gets the flow name — what a caller selects.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the steps, in the order they run, each with its settings for this flow.</summary>
    public required IReadOnlyList<AuthenticationFlowStep> Steps { get; init; }

    /// <summary>
    /// Gets the permissions a principal must hold to be issued anything by this flow; empty when the
    /// flow admits every active user.
    /// </summary>
    /// <remarks>
    /// Read off the flow's <see cref="IPermissionGatedAuthenticationStep"/> steps when it loads. The
    /// steps themselves enforce it at sign-in; this states the same requirement for code that must
    /// answer "could this user sign in here?" without running the flow.
    /// </remarks>
    public required IReadOnlyList<string> RequiredPermissions { get; init; }

    /// <summary>Gets the audience tokens from this flow are minted for.</summary>
    public required string Audience { get; init; }

    /// <summary>Gets the assurance level this flow demands before a token may be issued.</summary>
    public string? MinimumAcr { get; init; }

    /// <summary>Gets how long a flow suspended mid-way (e.g. awaiting an OIDC redirect) stays resumable.</summary>
    public required TimeSpan ExecutionLifetime { get; init; }

    /// <summary>
    /// Gets the secret manager holding the key that encrypts this flow's stored state: a suspended
    /// execution's context and the token behind a one-time browser code.
    /// </summary>
    /// <remarks>
    /// Null, together with <see cref="SecretKeyName"/>, for a flow that never stores state: no step
    /// federates and it has no <see cref="ReturnUrls"/> (password login, tenant switch). The provider
    /// refuses to load a flow that could store state without naming both, and a store asked to
    /// encrypt for a flow with neither fails rather than choosing a key.
    /// </remarks>
    public string? SecretManagerName { get; init; }

    /// <summary>Gets the key's name within <see cref="SecretManagerName"/>; null exactly when it is.</summary>
    public string? SecretKeyName { get; init; }

    /// <summary>
    /// Gets the absolute URLs a redirect sign-in may send the browser back to, exactly as the flow's row
    /// lists them. Null when the flow never returns a browser (password login, tenant switch); a caller
    /// asking such a flow for a return target is refused, never sent somewhere assumed. Match a URL with
    /// <see cref="AllowsReturnUrl"/>, not by comparing the strings.
    /// </summary>
    public IReadOnlyList<string>? ReturnUrls { get; init; }

    /// <summary>
    /// Returns whether this flow may send the browser back to <paramref name="returnUrl"/>: one of
    /// <see cref="ReturnUrls"/>, comparing both sides in one form, without a single trailing slash
    /// (<c>/api/v1/scalar</c> and <c>/api/v1/scalar/</c> are the same page to a caller). Nothing else is
    /// loosened: the rest of the URL, its query and its fragment must match exactly, because this list is
    /// what stops a redirect that carries a sign-in code from going to a stranger.
    /// </summary>
    /// <param name="returnUrl">The URL the caller asks to return to.</param>
    /// <returns>Whether it is one of this flow's return URLs.</returns>
    public bool AllowsReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl) || ReturnUrls is not { } allowed)
            return false;

        var wanted = WithoutTrailingSlash(returnUrl!);
        foreach (var url in allowed)
        {
            if (string.Equals(WithoutTrailingSlash(url), wanted, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    // The one convention: a return URL is compared without a single trailing slash. Exactly one, so
    // /a// is not /a.
    private static string WithoutTrailingSlash(string url)
        => url.Length > 1 && url[url.Length - 1] == '/' ? url.Substring(0, url.Length - 1) : url;

    /// <summary>
    /// Gets how long the one-time code this flow hands the browser stays redeemable. Set exactly when
    /// <see cref="ReturnUrls"/> is -- only a flow that returns a browser issues a code -- and null
    /// otherwise; the code store refuses to issue for a flow without one rather than choosing a lifetime.
    /// </summary>
    public TimeSpan? CodeLifetime { get; init; }

    /// <summary>
    /// Gets the Session authentication service this flow issues a server-side session into, or null
    /// when the flow returns a bearer token (FDW-903).
    /// </summary>
    /// <remarks>
    /// A session-delivered flow ends by storing the claims a token would have carried and handing the
    /// browser only a random id in a cookie. It stores state, so it names
    /// <see cref="SecretManagerName"/>/<see cref="SecretKeyName"/>. A redirect sign-in delivered this
    /// way still hands the browser a one-time code: the session id crosses to the browser app's origin
    /// behind it, and redeeming it there is what sets the cookie on that origin.
    /// </remarks>
    public string? SessionServiceName { get; init; }
}
