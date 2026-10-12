using System;
using Fdw.Configuration;

namespace Fdw.Services.ExternalIdentityProviders.Binding;

/// <summary>The contract every ExternalIdentity implementation carries.</summary>
public interface IExternalIdentityImplementationConfiguration : IImplementationConfiguration
{


    /// <summary>Gets or sets the issuer that asserts the subject.</summary>
    /// <remarks>
    /// The issuer identifier a token carries in its <c>iss</c> claim, not a sign-in flow's name and
    /// not an authority's display name. A binding is matched on exactly this value, so a row holding
    /// anything else resolves nothing.
    /// </remarks>
    string Issuer { get; set; }

    /// <summary>Gets or sets the subject that issuer proved.</summary>
    /// <remarks>
    /// Null only for an invitation not yet redeemed (see <see cref="InvitedEmail"/>): the operator
    /// knows who is coming but not the identifier the issuer will give them. The first sign-in that
    /// redeems the invitation fills it in, and from then on the row is an ordinary binding.
    /// </remarks>
    string? ExternalSubject { get; set; }

    /// <summary>
    /// Gets or sets the address an invitation was written for; null for a binding written with its subject.
    /// </summary>
    /// <remarks>
    /// Consulted only by a flow that opts in to invitations, only while <see cref="ExternalSubject"/>
    /// is null, and only against an address the issuer has verified. Matching on an address is the
    /// usual account-takeover path in federated sign-in, which is why this is never a general email
    /// match: it binds the one account an operator invited, for one issuer, once.
    /// </remarks>
    string? InvitedEmail { get; set; }

    /// <summary>Gets or sets the local user the subject resolves to.</summary>
    Guid UserId { get; set; }

    /// <summary>Gets or sets whether the binding is in force.</summary>
    bool IsActive { get; set; }
}
