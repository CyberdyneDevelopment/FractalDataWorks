using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Request to update a user.
/// </summary>
public class UpdateUserRequest
{
    /// <summary>
    /// Gets or sets the user name (bound from route). Endpoint resolves to the underlying user ID.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address, if updating.
    /// </summary>
    // Why: the client contract validates the format; without the matching server-side check an
    // invalid address is accepted here and only fails further downstream.
    [EmailAddress]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the active status, if updating.
    /// </summary>
    public bool? IsActive { get; set; }
}
