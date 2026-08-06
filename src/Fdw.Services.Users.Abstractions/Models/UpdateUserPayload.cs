using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Users.Clients.Models;

/// <summary>
/// Data transfer object payload for updating an existing user.
/// </summary>
// Why: pure payload payload, auto-properties only, no logic
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class UpdateUserPayload
{
    /// <summary>
    /// Gets or sets the updated email address for the user.
    /// </summary>
    [EmailAddress]
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user is active.
    /// </summary>
    public bool? IsActive { get; set; }
}
