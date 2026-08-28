using System.Collections.Generic;

namespace Fdw.Services.Authentication.Clients.Models;

/// <summary>
/// Client-side response model for the current authenticated user's information.
/// </summary>
public class GetMePayload
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public string UserId { get; set; } = "";

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string Username { get; set; } = "";

    /// <summary>
    /// Gets or sets the email address, if available.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the user's roles.
    /// </summary>
    public IList<string> Roles { get; set; } = [];

    /// <summary>
    /// Gets or sets the user's permissions.
    /// </summary>
    public IList<string> Permissions { get; set; } = [];
}
