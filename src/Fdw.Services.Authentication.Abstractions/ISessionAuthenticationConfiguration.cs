namespace Fdw.Services.Authentication.Abstractions;

/// <summary>
/// What the Session kind needs to read a server-side session's cookie and guard the writes it
/// authenticates (FDW-903).
/// </summary>
/// <remarks>
/// A session id is random and names no issuer, so <c>Authority</c> is never read for this kind. The
/// cookie's HttpOnly, Secure, Path=/ and host-only attributes are fixed by the option, not carried
/// here: relaxing any of them hands the id to script or to another host.
/// </remarks>
public interface ISessionAuthenticationConfiguration : IAuthenticationServiceImplementationConfiguration
{
    /// <summary>Gets or sets the cookie the session id travels in.</summary>
    string CookieName { get; set; }

    /// <summary>Gets or sets the cookie's SameSite mode: <c>Strict</c>, <c>Lax</c> or <c>None</c>, exactly.</summary>
    string SameSite { get; set; }

    /// <summary>
    /// Gets or sets the <c>SessionCsrfProtectionTypes</c> option that guards cookie-authenticated
    /// writes: <c>OriginCheck</c> or <c>SynchronizerToken</c>.
    /// </summary>
    string CsrfProtection { get; set; }

    /// <summary>
    /// Gets or sets the absolute origins a write may come from, whitespace-separated and matched
    /// exactly. Required by OriginCheck; null otherwise.
    /// </summary>
    string? AllowedOrigins { get; set; }

    /// <summary>
    /// Gets or sets the request header a write echoes its CSRF token in. Required by
    /// SynchronizerToken; null otherwise.
    /// </summary>
    string? CsrfHeaderName { get; set; }
}
