namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Request to bind an external identity to an account.
/// </summary>
public class BindExternalIdentityRequest : UserScopedRequest
{
    /// <summary>Gets or sets the issuer that asserts the identity — the <c>iss</c> a token from it carries.</summary>
    /// <remarks>
    /// Not a sign-in flow's name and not an authority's display name: a binding is matched on this
    /// value alone, so a row holding anything else resolves nothing. The accepted values are the
    /// <c>bindableIssuers</c> a sign-in-methods response carries, and one that is not among them is
    /// refused rather than written.
    /// </remarks>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets that issuer's subject identifier. Exactly one of this and
    /// <see cref="InvitedEmail"/> is required: a binding matches on the subject and never on an email,
    /// which is unverified, changes, and is the federated-login takeover path.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the address to invite when the subject is not known yet (FDW-910). Exactly one of
    /// this and <see cref="Subject"/> is required.
    /// </summary>
    /// <remarks>
    /// Writes an invitation, not a binding: the first sign-in from this issuer that carries this
    /// address verified, through a flow that accepts invitations, redeems it and becomes the binding.
    /// It is used once and never matches by address again.
    /// </remarks>
    public string? InvitedEmail { get; set; }
}
