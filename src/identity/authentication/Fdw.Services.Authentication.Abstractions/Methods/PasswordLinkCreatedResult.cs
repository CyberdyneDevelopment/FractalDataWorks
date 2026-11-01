using System;

namespace Fdw.Services.Authentication.Abstractions.Methods;

/// <summary>
/// Returned once when a set-password link is created (FDW-941).
/// Contains the raw token -- this is the only time it is exposed; only its peppered hash is stored.
/// </summary>
public sealed class PasswordLinkCreatedResult
{
    /// <summary>Gets or sets the link's id.</summary>
    public Guid LinkId { get; set; }

    /// <summary>Gets or sets the raw token the link carries. It cannot be read back later.</summary>
    public string RawToken { get; set; } = string.Empty;

    /// <summary>Gets or sets when the link stops working.</summary>
    public DateTimeOffset ExpiresAt { get; set; }
}
