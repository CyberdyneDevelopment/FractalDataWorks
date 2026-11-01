namespace Fdw.Services.Users.Clients.Models;

/// <summary>
/// An external-identity sign-in method to bind to a new user.
/// </summary>
/// <remarks>
/// Binding before the first sign-in is what makes that sign-in resolve to this account instead of
/// provisioning a second one. The subject is the issuer's own identifier for the person, and it
/// is the only thing a binding matches on: an email address is often unverified, changes, and can
/// be asserted for different people by different providers, which is the standard account-takeover
/// path in federated login.
/// </remarks>
public class ExternalSignInMethod
{
    /// <summary>
    /// Gets or sets the issuer that asserts the subject — the <c>iss</c> a token from it carries.
    /// </summary>
    /// <remarks>
    /// Not a sign-in flow's name and not an authority's display name. A binding is matched on this
    /// value alone, so a row holding anything else resolves nothing and the first sign-in provisions
    /// a second account instead. Read the accepted values from a sign-in-methods response's
    /// <c>bindableIssuers</c>.
    /// </remarks>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets that issuer's subject identifier for this account. Exactly one of this and
    /// <see cref="InvitedEmail"/> is required.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the address to invite, when the issuer's subject for this person is not known yet
    /// (FDW-910). Exactly one of this and <see cref="Subject"/> is required.
    /// </summary>
    /// <remarks>
    /// The invitation is redeemed by the first sign-in through a flow that accepts invitations, from
    /// this issuer, carrying this address verified; that sign-in's subject becomes the binding. A flow
    /// that does not accept invitations ignores it.
    /// </remarks>
    public string? InvitedEmail { get; set; }
}
