using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// Request to set settings of an existing notification (FDW-2931).
/// </summary>
public sealed class SetNotificationSettingsRequest
{
    /// <summary>Gets or sets the name of the notification.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the settings to set, by name. A secret setting is not given here: it is named on the notification.</summary>
    public Dictionary<string, JsonElement> Settings { get; set; } = new(StringComparer.Ordinal);
}
