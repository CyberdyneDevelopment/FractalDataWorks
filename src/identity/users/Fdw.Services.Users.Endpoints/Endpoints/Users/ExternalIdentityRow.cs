using System;
using Fdw.Services.ExternalIdentityProviders.Binding;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// The row an operator writes to give an account an external identity: a binding when the issuer's
/// subject is known, an invitation when only the person's address is (FDW-910).
/// </summary>
/// <remarks>
/// One place, because two routes write these rows (binding one later, and creating an account with
/// one), and the row's name is its address: a name built two ways is two rows for one identity.
/// </remarks>
public static class ExternalIdentityRow
{
    /// <summary>Builds the row. Exactly one of <paramref name="subject"/> and <paramref name="invitedEmail"/> is set.</summary>
    /// <param name="userId">The account.</param>
    /// <param name="issuer">The issuer that asserts the identity.</param>
    /// <param name="subject">The issuer's subject, when known.</param>
    /// <param name="invitedEmail">The address to invite, when the subject is not known.</param>
    /// <returns>The binding or invitation row.</returns>
    public static ExternalIdentityConfiguration For(Guid userId, string issuer, string? subject, string? invitedEmail)
    {
        if (string.IsNullOrWhiteSpace(subject) == string.IsNullOrWhiteSpace(invitedEmail))
            throw new ArgumentException("Exactly one of subject and invitedEmail is set; the request validation guarantees it.", nameof(subject));

        var isInvitation = string.IsNullOrWhiteSpace(subject);
        return new ExternalIdentityConfiguration
        {
            // One row per (Issuer, ExternalSubject) or per (Issuer, InvitedEmail), which are the
            // table's own unique indexes: naming rows after the issuer alone would let this one
            // overwrite another account's.
            Name = isInvitation
                ? ExternalIdentityConfiguration.InvitationName(issuer, invitedEmail!)
                : ExternalIdentityConfiguration.BindingName(issuer, subject!),
            Issuer = issuer,
            ExternalSubject = isInvitation ? null : subject,
            InvitedEmail = isInvitation ? invitedEmail : null,
            UserId = userId,
            IsActive = true,
        };
    }
}
