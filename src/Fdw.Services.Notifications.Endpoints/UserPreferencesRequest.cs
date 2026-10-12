using System;
using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// Request DTO that identifies a user for notification preferences.
/// </summary>
public sealed class UserPreferencesRequest
{
    /// <summary>Gets or sets the user unique identifier (from route).</summary>
    [Required]
    public Guid UserId { get; set; }
}
