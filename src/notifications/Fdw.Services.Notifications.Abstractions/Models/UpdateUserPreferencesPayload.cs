using System;
using System.Collections.Generic;

namespace Fdw.Services.Notifications.Clients.Models;

/// <summary>
/// Request to update notification preferences for a user.
/// </summary>
public sealed class UpdateUserPreferencesPayload
{
    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the notification preferences.
    /// </summary>
    public IReadOnlyList<UserNotificationPreferenceResponse> Preferences { get; set; } = [];
}
