using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// Request DTO for updating user notification preferences.
/// </summary>
public sealed class UpdateUserPreferencesRequest
{
    /// <summary>Gets or sets the user unique identifier (from route).</summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the list of preference updates.</summary>
    [Required]
    public IReadOnlyList<UserNotificationPreferenceDto> Preferences { get; set; } = [];
}
