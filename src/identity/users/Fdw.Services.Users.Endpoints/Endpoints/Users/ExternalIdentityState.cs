using System;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// An external identity bound to the account.
/// </summary>
public class ExternalIdentityState
{
    /// <summary>Gets or sets the binding's id, used to remove it.</summary>
    public Guid BindingId { get; set; }

    /// <summary>Gets or sets the issuer that asserts this identity — the <c>iss</c> it is matched on.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Gets or sets the subject the binding matches on; null for an invitation not yet redeemed.</summary>
    public string? Subject { get; set; }

    /// <summary>Gets or sets the address an invitation was written for; null for a binding written with its subject.</summary>
    public string? InvitedEmail { get; set; }

    /// <summary>Gets or sets whether the binding is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the binding's state: <c>Invited</c> while an invitation waits for its first sign-in,
    /// <c>Pending</c> for a binding written with its subject until the first sign-in through it,
    /// <c>Bound</c> afterwards.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Gets or sets when the first sign-in through this binding happened.</summary>
    public DateTimeOffset? BoundAt { get; set; }
}
