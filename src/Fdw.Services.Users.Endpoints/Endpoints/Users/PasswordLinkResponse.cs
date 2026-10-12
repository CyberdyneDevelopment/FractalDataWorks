using System;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// A newly created set-password link (FDW-941). The link is shown this once; it cannot be read back.
/// </summary>
public class PasswordLinkResponse
{
    /// <summary>Gets or sets the link to share. Whoever opens it can set the user's password once.</summary>
    public string Link { get; set; } = string.Empty;

    /// <summary>Gets or sets when the link stops working.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>Gets or sets whether the link was also emailed to the user.</summary>
    public bool Sent { get; set; }

    /// <summary>Gets or sets why the link was not emailed, when sending was asked for and failed.</summary>
    public string? NotSentReason { get; set; }
}
