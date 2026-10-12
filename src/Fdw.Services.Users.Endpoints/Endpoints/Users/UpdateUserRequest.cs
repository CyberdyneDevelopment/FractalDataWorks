using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Request to update a user.
/// </summary>
public class UpdateUserRequest
{
    /// <summary>
    /// Gets or sets the user's id or name (bound from the route, as it is on every <c>/users/{IdOrName}</c> route).
    /// The endpoint resolves it to the underlying user.
    /// </summary>
    public string IdOrName { get; set; } = string.Empty;

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
