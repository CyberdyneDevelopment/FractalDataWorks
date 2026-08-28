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
    [EmailAddress]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the active status, if updating.
    /// </summary>
    public bool? IsActive { get; set; }
}
