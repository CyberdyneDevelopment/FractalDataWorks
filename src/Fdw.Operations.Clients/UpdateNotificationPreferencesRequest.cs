namespace Fdw.Operations.Clients;

using System;
using System.Collections.Generic;

/// <summary>
/// Request DTO for updating user notification preferences.
/// </summary>
internal sealed class UpdateNotificationPreferencesRequest
{
    /// <summary>Gets or sets the user unique identifier.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the list of preference updates.</summary>
    public IReadOnlyList<NotificationPreferencePayload> Preferences { get; set; } = [];
}
