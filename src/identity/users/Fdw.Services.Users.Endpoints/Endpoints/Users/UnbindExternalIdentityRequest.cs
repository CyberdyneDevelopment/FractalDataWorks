using System;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Request to remove an external-identity binding from an account.
/// </summary>
public class UnbindExternalIdentityRequest : UserScopedRequest
{
    /// <summary>Gets or sets the binding to remove.</summary>
    public Guid BindingId { get; set; }
}
