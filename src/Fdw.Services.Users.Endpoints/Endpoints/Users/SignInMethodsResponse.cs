using System.Collections.Generic;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Every way an account can sign in, and what the calling token may do about each.
/// </summary>
/// <remarks>
/// One response rather than three, because a screen that shows the account's ways in needs all of
/// them at once, and because the capability flags depend on the whole picture -- whether a password
/// exists decides who may set one.
/// </remarks>
public class SignInMethodsResponse
{
    /// <summary>Gets or sets the password method.</summary>
    public PasswordMethodState Password { get; set; } = new();

    /// <summary>Gets or sets the external identities bound to the account.</summary>
    public IList<ExternalIdentityState> External { get; set; } = new List<ExternalIdentityState>();

    /// <summary>Gets or sets the account's API keys.</summary>
    public IList<ApiKeyState> Keys { get; set; } = new List<ApiKeyState>();

    /// <summary>Gets or sets whether the calling token may mint a key for this account.</summary>
    public bool CanMintKeys { get; set; }

    /// <summary>Gets or sets whether the calling token may bind an external identity to it.</summary>
    public bool CanBind { get; set; }

    /// <summary>
    /// Gets or sets the issuers a binding on this account may name.
    /// </summary>
    /// <remarks>
    /// A binding is matched against the issuer a proved subject carries, so these are the only
    /// values that can ever resolve a sign-in — not an authority's display name and not a flow's
    /// name. They travel with the flags rather than on the anonymous provider list, because that
    /// list serves a login screen, which does not need issuer URLs, while whoever may bind does.
    /// Empty when the caller may not bind, or when no authority could state its issuer.
    /// </remarks>
    public IList<string> BindableIssuers { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets whether <see cref="BindableIssuers"/> is every issuer this deployment has.
    /// </summary>
    /// <remarks>
    /// False when an authority could not state its issuer. Without this, a picker one issuer short
    /// looks exactly like a deployment that never had it: the administrator cannot find the provider
    /// they want and nothing on the screen says why. The list is still worth showing — the issuers
    /// in it are real — so the honest answer is to show it and say it may be short.
    /// </remarks>
    public bool BindableIssuersComplete { get; set; } = true;
}
