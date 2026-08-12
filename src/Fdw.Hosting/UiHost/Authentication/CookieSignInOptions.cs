using System.Collections.Generic;

namespace Fdw.Hosting.UiHost.Authentication;

/// <summary>
/// The values a skin supplies to the cookie sign-in routes.
/// </summary>
/// <remarks>
/// Everything else about exchanging a password for a Blazor cookie is identical in every skin over
/// an FDW API — post the OpenIddict password grant, read the token, build a principal from its
/// claims, store the tokens on the cookie. These four are the parts that genuinely belong to a
/// deployment, and none of them has a defensible default: a guessed client id authenticates as the
/// wrong application, and a guessed scheme name silently signs in to a scheme nothing reads.
/// </remarks>
public sealed class CookieSignInOptions
{
    /// <summary>Gets or sets the authentication scheme the cookie is issued under.</summary>
    public string Scheme { get; set; } = string.Empty;

    /// <summary>Gets or sets the OAuth client id presented to the token endpoint.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Gets or sets the scopes requested.</summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>Gets or sets the named HttpClient the token request is made through.</summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>Gets or sets the path a failed sign-in redirects back to.</summary>
    public string LoginPath { get; set; } = "/login";

    /// <summary>
    /// Gets the paths a caller may be returned to after signing in.
    /// </summary>
    /// <remarks>
    /// An allowlist rather than validation of whatever arrives, because returnUrl comes off the
    /// login form and is therefore attacker-controlled. A caller asking for a path not on this list
    /// is returned to the root: the value that reaches the redirect is always one this deployment
    /// declared, never one a caller supplied, so there is no string to sanitise and nothing to get
    /// wrong. Empty means every sign-in returns to the root.
    /// </remarks>
    public IList<string> ReturnPaths { get; } = new List<string>();

    /// <summary>Gets or sets how far the cookie outlives the access token.</summary>
    /// <remarks>
    /// The cookie deliberately outlives the token: OnValidatePrincipal refreshes the token during a
    /// session, and a cookie expiring with the token would end the session at the first refresh
    /// instead.
    /// </remarks>
    public int CookieLifetimeDays { get; set; } = 7;
}
