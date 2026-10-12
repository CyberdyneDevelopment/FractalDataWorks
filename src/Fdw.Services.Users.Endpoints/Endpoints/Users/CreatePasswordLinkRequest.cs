namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Request model for creating a set-password link for a user (FDW-941).
/// </summary>
public class CreatePasswordLinkRequest : UserScopedRequest
{
    /// <summary>
    /// Gets or sets whether to email the link to the user as well as returning it.
    /// </summary>
    /// <remarks>
    /// The link is returned either way, so an administrator can share it however suits. When the email
    /// cannot be sent, the response says why and the link still works.
    /// </remarks>
    public bool Send { get; set; }
}
